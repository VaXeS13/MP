using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Users;
using MP.Domain.Rentals;
using MP.Domain.Booths;
using MP.Domain.Carts;
using MP.Carts;
using MP.Domain.OrganizationalUnits;

namespace MP.Rentals
{
    public class RentalExtensionHandler : ITransientDependency
    {
        private readonly IRentalRepository _rentalRepository;
        private readonly IBoothRepository _boothRepository;
        private readonly ICartRepository _cartRepository;
        private readonly IRepository<RentalExtensionPayment, Guid> _extensionPaymentRepository;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ICurrentUser _currentUser;
        private readonly CartManager _cartManager;
        private readonly ICurrentOrganizationalUnit _currentOrganizationalUnit;

        public RentalExtensionHandler(
            IRentalRepository rentalRepository,
            IBoothRepository boothRepository,
            ICartRepository cartRepository,
            IRepository<RentalExtensionPayment, Guid> extensionPaymentRepository,
            IGuidGenerator guidGenerator,
            ICurrentUser currentUser,
            CartManager cartManager,
            ICurrentOrganizationalUnit currentOrganizationalUnit)
        {
            _rentalRepository = rentalRepository;
            _boothRepository = boothRepository;
            _cartRepository = cartRepository;
            _extensionPaymentRepository = extensionPaymentRepository;
            _guidGenerator = guidGenerator;
            _currentUser = currentUser;
            _cartManager = cartManager;
            _currentOrganizationalUnit = currentOrganizationalUnit;
        }

        public async Task<Rental> HandleFreeExtensionAsync(Rental rental, DateTime newEndDate)
        {
            var newPeriod = new RentalPeriod(rental.Period.StartDate, newEndDate);
            rental.ExtendRental(newPeriod, 0);
            await _rentalRepository.UpdateAsync(rental);

            await LogExtensionAsync(rental.Id, rental.Period.EndDate, newEndDate, 0, rental.Currency, ExtensionPaymentType.Free);

            return rental;
        }

        public async Task<Rental> HandleCashExtensionAsync(Rental rental, DateTime newEndDate, decimal cost)
        {
            var newPeriod = new RentalPeriod(rental.Period.StartDate, newEndDate);
            rental.ExtendRental(newPeriod, cost);
            rental.MarkAsPaid(cost, DateTime.Now, "CASH_PAYMENT");

            await _rentalRepository.UpdateAsync(rental);
            await LogExtensionAsync(rental.Id, rental.Period.EndDate, newEndDate, cost, rental.Currency, ExtensionPaymentType.Cash);

            return rental;
        }

        public async Task<Rental> HandleTerminalExtensionAsync(
            Rental rental,
            DateTime newEndDate,
            decimal cost,
            string? transactionId,
            string? receiptNumber)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
                throw new BusinessException("TERMINAL_TRANSACTION_ID_REQUIRED");

            var newPeriod = new RentalPeriod(rental.Period.StartDate, newEndDate);
            rental.ExtendRental(newPeriod, cost);
            rental.MarkAsPaid(cost, DateTime.Now, transactionId);
            rental.Payment.SetTerminalDetails(transactionId, receiptNumber);

            await _rentalRepository.UpdateAsync(rental);
            await LogExtensionAsync(
                rental.Id,
                rental.Period.EndDate,
                newEndDate,
                cost,
                rental.Currency,
                ExtensionPaymentType.Terminal,
                transactionId,
                receiptNumber);

            return rental;
        }

        public async Task<Rental> HandleOnlineExtensionAsync(
            Rental rental,
            DateTime newEndDate,
            decimal cost,
            int? timeoutMinutes)
        {
            var organizationalUnitId = _currentOrganizationalUnit.Id ?? throw new BusinessException("ORGANIZATIONAL_UNIT_REQUIRED")
                .WithData("message", "Current organizational unit context is not set");

            // Get or create cart for user
            var cart = await _cartManager.GetOrCreateCartAsync(rental.UserId, organizationalUnitId);

            // Set timeout
            var timeout = timeoutMinutes ?? 30;
            cart.SetExtensionTimeout(DateTime.Now.AddMinutes(timeout));

            // Create CartItem for extension
            var cartItem = new CartItem(
                _guidGenerator.Create(),
                cart.Id,
                organizationalUnitId,
                rental.BoothId,
                rental.BoothTypeId,
                rental.Period.StartDate,
                newEndDate,
                cost / ((newEndDate - rental.Period.EndDate).Days),
                rental.Currency,
                CartItemType.Extension,
                rental.Id
            );

            cart.AddItem(cartItem);

            await _cartRepository.UpdateAsync(cart);

            return rental;
        }

        private async Task LogExtensionAsync(
            Guid rentalId,
            DateTime oldEndDate,
            DateTime newEndDate,
            decimal cost,
            Currency currency,
            ExtensionPaymentType paymentType,
            string? transactionId = null,
            string? receiptNumber = null)
        {
            var organizationalUnitId = _currentOrganizationalUnit.Id ?? throw new BusinessException("ORGANIZATIONAL_UNIT_REQUIRED")
                .WithData("message", "Current organizational unit context is not set");

            var log = new RentalExtensionPayment(
                _guidGenerator.Create(),
                rentalId,
                organizationalUnitId,
                oldEndDate,
                newEndDate,
                cost,
                currency,
                paymentType,
                _currentUser.GetId(),
                transactionId,
                receiptNumber
            );

            await _extensionPaymentRepository.InsertAsync(log);
        }
    }
}
