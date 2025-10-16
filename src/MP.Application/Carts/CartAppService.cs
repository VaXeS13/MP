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
using Volo.Abp.EventBus.Local;
using MP.Permissions;
using MP.Domain.Carts;
using MP.Domain.Booths;
using MP.Domain.Rentals;
using MP.Domain.BoothTypes;
using MP.Rentals;
using MP.Domain.Promotions;
using MP.Domain.Payments.Events;

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
        private readonly MP.Domain.Promotions.IPromotionRepository _promotionRepository;
        private readonly PromotionManager _promotionManager;
        private readonly ILocalEventBus _localEventBus;

        public CartAppService(
            ICartRepository cartRepository,
            IBoothRepository boothRepository,
            IBoothTypeRepository boothTypeRepository,
            CartManager cartManager,
            RentalManager rentalManager,
            IRentalRepository rentalRepository,
            MP.Domain.Promotions.IPromotionRepository promotionRepository,
            PromotionManager promotionManager,
            ILocalEventBus localEventBus)
        {
            _cartRepository = cartRepository;
            _boothRepository = boothRepository;
            _boothTypeRepository = boothTypeRepository;
            _cartManager = cartManager;
            _rentalManager = rentalManager;
            _rentalRepository = rentalRepository;
            _promotionRepository = promotionRepository;
            _promotionManager = promotionManager;
            _localEventBus = localEventBus;
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

            // Find the item to check if it has a linked Rental
            var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
            if (item != null && item.RentalId.HasValue)
            {
                // If item has linked Rental (admin-created with online payment), soft delete it
                try
                {
                    var rental = await _rentalRepository.GetAsync(item.RentalId.Value);
                    if (rental.Status == RentalStatus.Draft)
                    {
                        await _rentalRepository.DeleteAsync(rental);
                        Logger.LogInformation("Deleted Draft Rental {RentalId} when user removed CartItem {CartItemId}",
                            rental.Id, itemId);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to delete Rental {RentalId} when removing CartItem {CartItemId}",
                        item.RentalId, itemId);
                    // Continue with cart item removal even if rental deletion fails
                }
            }

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

            // Delete all linked Draft Rentals before clearing cart
            var itemsWithRentals = cart.Items.Where(i => i.RentalId.HasValue).ToList();
            foreach (var item in itemsWithRentals)
            {
                try
                {
                    var rental = await _rentalRepository.GetAsync(item.RentalId!.Value);
                    if (rental.Status == RentalStatus.Draft)
                    {
                        await _rentalRepository.DeleteAsync(rental);
                        Logger.LogInformation("Deleted Draft Rental {RentalId} when clearing cart {CartId}",
                            rental.Id, cart.Id);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to delete Rental {RentalId} when clearing cart",
                        item.RentalId);
                    // Continue with clearing cart even if rental deletion fails
                }
            }

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

                // Check for expired items and validate availability
                var expiredItems = cart.Items.Where(i => i.IsReservationExpired()).ToList();
                if (expiredItems.Any())
                {
                    // Revalidate expired items - they may no longer be available
                    var unavailableItems = new List<string>();
                    foreach (var item in expiredItems)
                    {
                        try
                        {
                            await _cartManager.ValidateCartItemAsync(
                                item.BoothId,
                                item.StartDate,
                                item.EndDate,
                                cart.Id
                            );
                        }
                        catch (BusinessException)
                        {
                            var booth = await _boothRepository.GetAsync(item.BoothId);
                            unavailableItems.Add($"Booth {booth.Number}");
                        }
                    }

                    if (unavailableItems.Any())
                    {
                        return new CheckoutResultDto
                        {
                            Success = false,
                            ErrorMessage = $"The following booths are no longer available: {string.Join(", ", unavailableItems)}. Please remove them from your cart."
                        };
                    }

                    Logger.LogInformation("Checkout: Revalidated {ExpiredItemCount} expired items - all still available", expiredItems.Count);
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

                // Validate promotion if applied
                if (cart.HasPromotionApplied())
                {
                    try
                    {
                        var promotion = await _promotionRepository.GetAsync(cart.AppliedPromotionId!.Value);

                        // Check if promotion is still valid
                        if (!promotion.IsValid())
                        {
                            cart.RemovePromotion();
                            await _cartRepository.UpdateAsync(cart);

                            Logger.LogWarning("Promotion {PromotionId} is no longer valid during checkout for cart {CartId}",
                                cart.AppliedPromotionId, cart.Id);

                            return new CheckoutResultDto
                            {
                                Success = false,
                                ErrorMessage = L["Promotion:NoLongerValid"]
                            };
                        }

                        // Validate promotion for cart (minimum booths, booth types, per-user limit)
                        await _promotionManager.ValidateAndApplyToCartAsync(cart, cart.PromoCodeUsed);
                    }
                    catch (BusinessException ex) when (ex.Code == "PROMOTION_NOT_VALID" ||
                                                       ex.Code == "PROMOTION_MINIMUM_BOOTHS_NOT_MET" ||
                                                       ex.Code == "PROMOTION_NOT_APPLICABLE_TO_BOOTH_TYPES" ||
                                                       ex.Code == "PROMOTION_USER_LIMIT_EXCEEDED")
                    {
                        cart.RemovePromotion();
                        await _cartRepository.UpdateAsync(cart);

                        Logger.LogWarning("Promotion validation failed during checkout: {ErrorCode}", ex.Code);

                        return new CheckoutResultDto
                        {
                            Success = false,
                            ErrorMessage = L["Promotion:ValidationFailed"]
                        };
                    }
                    catch (EntityNotFoundException)
                    {
                        cart.RemovePromotion();
                        await _cartRepository.UpdateAsync(cart);

                        Logger.LogWarning("Promotion {PromotionId} not found during checkout for cart {CartId}",
                            cart.AppliedPromotionId, cart.Id);

                        return new CheckoutResultDto
                        {
                            Success = false,
                            ErrorMessage = L["Promotion:NotFound"]
                        };
                    }
                }

                // Create all rentals or use existing ones
                var rentalIds = new List<Guid>();

                foreach (var item in cart.Items)
                {
                    var booth = await _boothRepository.GetAsync(item.BoothId);
                    Rental rental;

                    // Check if CartItem has pre-created Rental (from admin)
                    if (item.RentalId.HasValue)
                    {
                        // Use existing Rental created by admin
                        rental = await _rentalRepository.GetAsync(item.RentalId.Value);

                        // Verify rental is still in Draft status
                        if (rental.Status != RentalStatus.Draft)
                            throw new BusinessException("RENTAL_ALREADY_PROCESSED")
                                .WithData("RentalId", rental.Id)
                                .WithData("Status", rental.Status);
                    }
                    else
                    {
                        // Create new rental (normal user flow)
                        rental = await _rentalManager.CreateRentalAsync(
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
                    }

                    rentalIds.Add(rental.Id);
                }

                // Use final amount from cart (includes promotion discount)
                decimal totalAmount = cart.GetFinalAmount();

                // Force save rentals before payment
                await UnitOfWorkManager.Current!.SaveChangesAsync();

                // Get currency from first cart item (all items should have same currency - tenant currency)
                var currency = cart.Items.First().Currency.ToString();

                // Create single payment for all rentals
                var paymentRequest = new MP.Application.Contracts.Payments.CreatePaymentRequestDto
                {
                    Amount = totalAmount,
                    Currency = currency,
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

                // Publish PaymentInitiated event for notification
                await _localEventBus.PublishAsync(new PaymentInitiatedEvent
                {
                    UserId = userId,
                    TransactionId = paymentResult.TransactionId ?? string.Empty,
                    SessionId = paymentResult.SessionId ?? paymentResult.TransactionId ?? Guid.NewGuid().ToString(),
                    Amount = totalAmount,
                    Currency = currency,
                    RentalIds = rentalIds,
                    InitiatedAt = DateTime.UtcNow
                });

                Logger.LogInformation("Published PaymentInitiatedEvent for user {UserId}, transaction {TransactionId}",
                    userId, paymentResult.TransactionId);

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

            // Add promotion data if applied
            if (cart.HasPromotionApplied())
            {
                dto.AppliedPromotionId = cart.AppliedPromotionId;
                dto.DiscountAmount = cart.DiscountAmount;
                dto.PromoCodeUsed = cart.PromoCodeUsed;

                // Get promotion name
                if (cart.AppliedPromotionId.HasValue)
                {
                    try
                    {
                        var promotion = await _promotionRepository.GetAsync(cart.AppliedPromotionId.Value);
                        dto.PromotionName = promotion.Name;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Failed to load promotion {PromotionId} for cart {CartId}",
                            cart.AppliedPromotionId, cart.Id);
                    }
                }
            }

            // Calculate per-item discount (proportional to item price)
            var totalAmount = cart.GetTotalAmount();
            var totalDiscount = cart.DiscountAmount;

            // Map items with related data
            foreach (var item in cart.Items)
            {
                var booth = await _boothRepository.GetAsync(item.BoothId);
                var boothType = await _boothTypeRepository.GetAsync(item.BoothTypeId);

                var itemTotalPrice = item.GetTotalPrice();

                // Calculate proportional discount for this item
                decimal itemDiscount = 0;
                if (totalAmount > 0 && totalDiscount > 0)
                {
                    itemDiscount = (itemTotalPrice / totalAmount) * totalDiscount;
                }

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
                    TotalPrice = itemTotalPrice,
                    DiscountAmount = itemDiscount,
                    FinalPrice = itemTotalPrice - itemDiscount,
                    BoothNumber = booth.Number,
                    BoothDescription = $"Booth {booth.Number}",
                    BoothTypeName = boothType.Name,
                    Currency = item.Currency.ToString(),
                    ReservationExpiresAt = item.ReservationExpiresAt,
                    IsExpired = item.IsReservationExpired()
                };

                dto.Items.Add(itemDto);
            }

            return dto;
        }
    }
}