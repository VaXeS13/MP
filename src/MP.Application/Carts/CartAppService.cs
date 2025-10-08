using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Users;
using Volo.Abp.Uow;
using MP.Permissions;
using MP.Domain.Carts;
using MP.Domain.Booths;
using MP.Domain.Rentals;
using MP.Domain.BoothTypes;

namespace MP.Carts
{
    [Authorize(MPPermissions.Rentals.Default)]
    public class CartAppService : ApplicationService, ICartAppService
    {
        private readonly ICartRepository _cartRepository;
        private readonly IBoothRepository _boothRepository;
        private readonly IBoothTypeRepository _boothTypeRepository;
        private readonly CartManager _cartManager;
        private readonly RentalManager _rentalManager;
        private readonly IRentalRepository _rentalRepository;

        public CartAppService(
            ICartRepository cartRepository,
            IBoothRepository boothRepository,
            IBoothTypeRepository boothTypeRepository,
            CartManager cartManager,
            RentalManager rentalManager,
            IRentalRepository rentalRepository)
        {
            _cartRepository = cartRepository;
            _boothRepository = boothRepository;
            _boothTypeRepository = boothTypeRepository;
            _cartManager = cartManager;
            _rentalManager = rentalManager;
            _rentalRepository = rentalRepository;
        }

        public async Task<CartDto> GetMyCartAsync()
        {
            var userId = CurrentUser.GetId();
            var cart = await _cartManager.GetOrCreateActiveCartAsync(userId, CurrentTenant.Id);

            return await MapToCartDtoAsync(cart);
        }

        public async Task<CartDto> AddItemAsync(AddToCartDto input)
        {
            var userId = CurrentUser.GetId();
            var cart = await _cartManager.GetOrCreateActiveCartAsync(userId, CurrentTenant.Id);

            // Add item with validation
            await _cartManager.AddItemToCartAsync(
                cart,
                input.BoothId,
                input.BoothTypeId,
                input.StartDate,
                input.EndDate,
                input.Notes
            );

            await _cartRepository.UpdateAsync(cart);

            return await MapToCartDtoAsync(cart);
        }

        public async Task<CartDto> UpdateItemAsync(Guid itemId, UpdateCartItemDto input)
        {
            var userId = CurrentUser.GetId();
            var cart = await _cartRepository.GetActiveCartByUserIdAsync(userId, includeItems: true);

            if (cart == null)
                throw new BusinessException("CART_NOT_FOUND");

            // Update item with validation
            await _cartManager.UpdateCartItemAsync(
                cart,
                itemId,
                input.BoothTypeId,
                input.StartDate,
                input.EndDate,
                input.Notes
            );

            await _cartRepository.UpdateAsync(cart);

            return await MapToCartDtoAsync(cart);
        }

        public async Task<CartDto> RemoveItemAsync(Guid itemId)
        {
            var userId = CurrentUser.GetId();
            var cart = await _cartRepository.GetActiveCartByUserIdAsync(userId, includeItems: true);

            if (cart == null)
                throw new BusinessException("CART_NOT_FOUND");

            cart.RemoveItem(itemId);

            await _cartRepository.UpdateAsync(cart);

            return await MapToCartDtoAsync(cart);
        }

        public async Task<CartDto> ClearCartAsync()
        {
            var userId = CurrentUser.GetId();
            var cart = await _cartRepository.GetActiveCartByUserIdAsync(userId, includeItems: true);

            if (cart == null)
                throw new BusinessException("CART_NOT_FOUND");

            cart.Clear();

            await _cartRepository.UpdateAsync(cart);

            return await MapToCartDtoAsync(cart);
        }

        [UnitOfWork]
        public virtual async Task<CheckoutResultDto> CheckoutAsync(CheckoutCartDto input)
        {
            try
            {
                var userId = CurrentUser.GetId();
                var cart = await _cartRepository.GetActiveCartByUserIdAsync(userId, includeItems: true);

                if (cart == null || cart.IsEmpty())
                {
                    return new CheckoutResultDto
                    {
                        Success = false,
                        ErrorMessage = "Cart is empty"
                    };
                }

                // Validate all items are still available
                foreach (var item in cart.Items)
                {
                    await _cartManager.ValidateCartItemAsync(
                        item.BoothId,
                        item.StartDate,
                        item.EndDate,
                        cart.Id
                    );
                }

                // Create all rentals
                var rentalIds = new List<Guid>();
                decimal totalAmount = 0;

                foreach (var item in cart.Items)
                {
                    var booth = await _boothRepository.GetAsync(item.BoothId);

                    // Create rental
                    var rental = await _rentalManager.CreateRentalAsync(
                        userId,
                        item.BoothId,
                        item.BoothTypeId,
                        item.StartDate,
                        item.EndDate,
                        booth.PricePerDay
                    );

                    rental.SetNotes(item.Notes);

                    await _rentalRepository.InsertAsync(rental);
                    await _boothRepository.UpdateAsync(booth);

                    rentalIds.Add(rental.Id);
                    totalAmount += rental.Payment.TotalAmount;
                }

                // Force save rentals before payment
                await UnitOfWorkManager.Current!.SaveChangesAsync();

                // Create single payment for all rentals
                var paymentRequest = new MP.Application.Contracts.Payments.CreatePaymentRequestDto
                {
                    Amount = totalAmount,
                    Currency = "PLN", // TODO: Get from booth or tenant settings
                    Description = $"Cart checkout - {cart.Items.Count} booth rental(s)",
                    ProviderId = input.PaymentProviderId,
                    MethodId = input.PaymentMethodId,
                    Metadata = new Dictionary<string, object>
                    {
                        ["cartId"] = cart.Id,
                        ["rentalIds"] = rentalIds,
                        ["itemCount"] = cart.Items.Count
                    }
                };

                var paymentProviderService = LazyServiceProvider.LazyGetRequiredService<MP.Application.Contracts.Payments.IPaymentProviderAppService>();
                var paymentResult = await paymentProviderService.CreatePaymentAsync(paymentRequest);

                if (!paymentResult.Success)
                {
                    // Payment creation failed - rollback by cancelling rentals
                    foreach (var rentalId in rentalIds)
                    {
                        var rental = await _rentalRepository.GetAsync(rentalId);
                        rental.Cancel("Payment creation failed during checkout");
                        await _rentalRepository.UpdateAsync(rental);
                    }

                    return new CheckoutResultDto
                    {
                        Success = false,
                        ErrorMessage = paymentResult.ErrorMessage ?? "Payment creation failed"
                    };
                }

                // Mark cart as checked out
                cart.MarkAsCheckedOut();
                await _cartRepository.UpdateAsync(cart);

                return new CheckoutResultDto
                {
                    Success = true,
                    TransactionId = paymentResult.TransactionId,
                    PaymentUrl = paymentResult.PaymentUrl,
                    RentalIds = rentalIds,
                    TotalAmount = totalAmount,
                    ItemCount = cart.Items.Count
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during cart checkout");
                return new CheckoutResultDto
                {
                    Success = false,
                    ErrorMessage = "An error occurred during checkout"
                };
            }
        }

        private async Task<CartDto> MapToCartDtoAsync(Cart cart)
        {
            var dto = new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                Status = cart.Status,
                StatusDisplayName = cart.Status.ToString(),
                ItemCount = cart.GetItemCount(),
                TotalAmount = cart.GetTotalAmount(),
                TotalDays = cart.GetTotalDays(),
                CreationTime = cart.CreationTime,
                LastModificationTime = cart.LastModificationTime
            };

            // Map items with related data
            foreach (var item in cart.Items)
            {
                var booth = await _boothRepository.GetAsync(item.BoothId);
                var boothType = await _boothTypeRepository.GetAsync(item.BoothTypeId);

                var itemDto = new CartItemDto
                {
                    Id = item.Id,
                    CartId = item.CartId,
                    BoothId = item.BoothId,
                    BoothTypeId = item.BoothTypeId,
                    StartDate = item.StartDate,
                    EndDate = item.EndDate,
                    PricePerDay = item.PricePerDay,
                    Notes = item.Notes,
                    DaysCount = item.GetDaysCount(),
                    TotalPrice = item.GetTotalPrice(),
                    BoothNumber = booth.Number,
                    BoothDescription = $"Booth {booth.Number}",
                    BoothTypeName = boothType.Name,
                    Currency = booth.Currency.ToString()
                };

                dto.Items.Add(itemDto);
            }

            return dto;
        }
    }
}