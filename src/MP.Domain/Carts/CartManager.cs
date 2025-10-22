using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;
using Volo.Abp.Settings;
using MP.Carts;
using MP.Domain.Booths;
using MP.Domain.Rentals;
using MP.Domain.Settings;

namespace MP.Domain.Carts
{
    public class CartManager : DomainService
    {
        private readonly ICartRepository _cartRepository;
        private readonly IBoothRepository _boothRepository;
        private readonly IRentalRepository _rentalRepository;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ISettingProvider _settingProvider;

        public CartManager(
            ICartRepository cartRepository,
            IBoothRepository boothRepository,
            IRentalRepository rentalRepository,
            IGuidGenerator guidGenerator,
            ISettingProvider settingProvider)
        {
            _cartRepository = cartRepository;
            _boothRepository = boothRepository;
            _rentalRepository = rentalRepository;
            _guidGenerator = guidGenerator;
            _settingProvider = settingProvider;
        }

        /// <summary>
        /// Gets or creates an active cart for a user
        /// </summary>
        public async Task<Cart> GetOrCreateActiveCartAsync(Guid userId, Guid? tenantId = null)
        {
            var cart = await _cartRepository.GetActiveCartByUserIdAsync(userId, includeItems: true);

            if (cart == null)
            {
                cart = new Cart(_guidGenerator.Create(), userId, tenantId);
                await _cartRepository.InsertAsync(cart);
            }

            return cart;
        }

        /// <summary>
        /// Alias for GetOrCreateActiveCartAsync
        /// </summary>
        public async Task<Cart> GetOrCreateCartAsync(Guid userId, Guid? tenantId = null)
        {
            return await GetOrCreateActiveCartAsync(userId, tenantId);
        }

        /// <summary>
        /// Validates if a booth can be added to cart for the given period
        /// Checks for active rentals AND active reservations in other users' carts
        /// </summary>
        public async Task ValidateCartItemAsync(
            Guid boothId,
            DateTime startDate,
            DateTime endDate,
            Guid? excludeCartId = null)
        {
            // Check if booth exists
            var booth = await _boothRepository.GetAsync(boothId);

            // Check if start date is not in the past
            var today = DateTime.Today;
            if (startDate.Date < today)
            {
                throw new BusinessException("RENTAL_START_DATE_IN_PAST")
                    .WithData("BoothId", boothId)
                    .WithData("StartDate", startDate)
                    .WithData("Today", today);
            }

            // Check if booth has any active rentals in this period
            var hasActiveRental = await _rentalRepository.HasActiveRentalForBoothAsync(
                boothId, startDate, endDate);

            if (hasActiveRental)
            {
                throw new BusinessException("BOOTH_ALREADY_RENTED_IN_PERIOD")
                    .WithData("BoothId", boothId)
                    .WithData("StartDate", startDate)
                    .WithData("EndDate", endDate);
            }

            // NEW: Check if booth has active reservations in other users' carts
            var hasActiveReservation = await HasActiveReservationAsync(boothId, startDate, endDate, excludeCartId);
            if (hasActiveReservation)
            {
                throw new BusinessException("BOOTH_RESERVED_BY_ANOTHER_USER")
                    .WithData("BoothId", boothId)
                    .WithData("StartDate", startDate)
                    .WithData("EndDate", endDate);
            }

            // Validate minimum rental period
            var minimumRentalDays = await GetMinimumRentalDaysAsync();
            var days = (endDate - startDate).Days + 1;
            if (days < minimumRentalDays)
            {
                throw new BusinessException("RENTAL_PERIOD_TOO_SHORT")
                    .WithData("Days", days)
                    .WithData("MinimumDays", minimumRentalDays);
            }

            // Validate minimum gap between rentals
            await ValidateMinimumGapAsync(boothId, startDate, endDate);
        }

        /// <summary>
        /// Checks if booth has active (non-expired) reservations in other users' carts
        /// </summary>
        private async Task<bool> HasActiveReservationAsync(
            Guid boothId,
            DateTime startDate,
            DateTime endDate,
            Guid? excludeCartId = null)
        {
            var now = DateTime.Now;
            var queryable = await _cartRepository.GetQueryableAsync();

            var hasReservation = queryable
                .Where(c => c.Status == CartStatus.Active)
                .Where(c => !excludeCartId.HasValue || c.Id != excludeCartId.Value)
                .SelectMany(c => c.Items)
                .Any(item =>
                    item.BoothId == boothId &&
                    item.StartDate <= endDate &&
                    item.EndDate >= startDate &&
                    item.ReservationExpiresAt.HasValue &&
                    item.ReservationExpiresAt.Value >= now); // Only active reservations

            return hasReservation;
        }

        /// <summary>
        /// Validates that the rental doesn't create an unusable gap (too small to rent)
        /// </summary>
        private async Task ValidateMinimumGapAsync(Guid boothId, DateTime startDate, DateTime endDate)
        {
            var minimumGapDays = await GetMinimumGapDaysAsync();

            if (minimumGapDays == 0)
            {
                return; // Gap validation disabled
            }

            // Check for rental before the requested period
            var rentalBefore = await _rentalRepository.GetNearestRentalBeforeAsync(boothId, startDate);
            if (rentalBefore != null)
            {
                var today = DateTime.Today;
                var daysBefore = (startDate.Date - rentalBefore.Period.EndDate.Date).Days - 1;

                // If the previous rental ended in the past, don't enforce gap validation
                // The gap has already been "wasted" by the passage of time
                if (rentalBefore.Period.EndDate.Date >= today)
                {
                    // If there's a gap, it must be either 0 (adjacent) or >= minimumGapDays
                    if (daysBefore > 0 && daysBefore < minimumGapDays)
                    {
                        throw new BusinessException("RENTAL_CREATES_UNUSABLE_GAP_BEFORE")
                            .WithData("BoothId", boothId)
                            .WithData("StartDate", startDate)
                            .WithData("PreviousRentalEndDate", rentalBefore.Period.EndDate)
                            .WithData("GapDays", daysBefore)
                            .WithData("MinimumGapDays", minimumGapDays)
                            .WithData("SuggestedStartDate", rentalBefore.Period.EndDate.AddDays(1))
                            .WithData("AlternativeStartDate", rentalBefore.Period.EndDate.AddDays(minimumGapDays + 1));
                    }
                }
            }

            // Check for rental after the requested period
            var rentalAfter = await _rentalRepository.GetNearestRentalAfterAsync(boothId, endDate);
            if (rentalAfter != null)
            {
                var daysAfter = (rentalAfter.Period.StartDate.Date - endDate.Date).Days - 1;

                // If there's a gap, it must be either 0 (adjacent) or >= minimumGapDays
                if (daysAfter > 0 && daysAfter < minimumGapDays)
                {
                    throw new BusinessException("RENTAL_CREATES_UNUSABLE_GAP_AFTER")
                        .WithData("BoothId", boothId)
                        .WithData("EndDate", endDate)
                        .WithData("NextRentalStartDate", rentalAfter.Period.StartDate)
                        .WithData("GapDays", daysAfter)
                        .WithData("MinimumGapDays", minimumGapDays)
                        .WithData("SuggestedEndDate", rentalAfter.Period.StartDate.AddDays(-1))
                        .WithData("AlternativeEndDate", rentalAfter.Period.StartDate.AddDays(-minimumGapDays - 1));
                }
            }
        }

        /// <summary>
        /// Gets the minimum gap days setting
        /// </summary>
        private async Task<int> GetMinimumGapDaysAsync()
        {
            var setting = await _settingProvider.GetOrNullAsync(MPSettings.Booths.MinimumGapDays);
            return int.TryParse(setting, out var gap) ? gap : 7;
        }

        /// <summary>
        /// Gets the minimum rental days setting
        /// </summary>
        private async Task<int> GetMinimumRentalDaysAsync()
        {
            var setting = await _settingProvider.GetOrNullAsync(MPSettings.Booths.MinimumRentalDays);
            return int.TryParse(setting, out var days) ? days : 7;
        }

        /// <summary>
        /// Adds an item to the cart with validation and automatic 5-minute reservation
        /// </summary>
        public async Task<CartItem> AddItemToCartAsync(
            Cart cart,
            Guid boothId,
            Guid boothTypeId,
            DateTime startDate,
            DateTime endDate,
            string? notes = null,
            int? reservationMinutes = null)
        {
            // Validate the cart item
            await ValidateCartItemAsync(boothId, startDate, endDate, cart.Id);

            // Get booth for pricing
            var booth = await _boothRepository.GetAsync(boothId);

            // Calculate price using new multi-period pricing system
            var days = (endDate - startDate).Days + 1;

            // Calculate exact total price to avoid rounding errors (10 zł / 30 days → 0.333... → 9.90 zł)
            var actualTotalPrice = CalculateTotalPrice(booth, days);
            var pricePerDay = actualTotalPrice / days;

            // Get tenant currency
            var currency = await GetTenantCurrencyAsync();

            // Set reservation expiration (default 5 minutes for normal users)
            var reservationExpires = DateTime.Now.AddMinutes(reservationMinutes ?? 5);

            // Add item to cart with reservation and tenant currency
            var itemId = _guidGenerator.Create();
            var item = cart.AddItem(
                itemId,
                boothId,
                boothTypeId,
                startDate,
                endDate,
                pricePerDay,
                currency,
                reservationExpires,
                notes
            );

            // Store the exact total price to prevent rounding errors when displayed
            item.SetStoredTotalPrice(actualTotalPrice);

            return item;
        }

        /// <summary>
        /// Calculates the total price for a rental period.
        /// Uses multi-period pricing if available, falls back to legacy PricePerDay
        /// </summary>
        private decimal CalculateTotalPrice(Booth booth, int days)
        {
            // Use new pricing system if booth has pricing periods
            if (booth.PricingPeriods != null && booth.PricingPeriods.Count > 0)
            {
                var calculation = booth.CalculatePrice(days);
                // Return exact total price (not average per day)
                return calculation.TotalPrice;
            }

            // Fall back to legacy pricing
#pragma warning disable CS0618 // Type or member is obsolete
            return days * booth.PricePerDay;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Updates a cart item with validation
        /// </summary>
        public async Task UpdateCartItemAsync(
            Cart cart,
            Guid itemId,
            Guid boothTypeId,
            DateTime startDate,
            DateTime endDate,
            string? notes)
        {
            var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null)
            {
                throw new BusinessException("CART_ITEM_NOT_FOUND")
                    .WithData("ItemId", itemId);
            }

            // Validate the updated period
            await ValidateCartItemAsync(item.BoothId, startDate, endDate, cart.Id);

            // Update the item
            cart.UpdateItem(itemId, boothTypeId, startDate, endDate, notes);
        }

        /// <summary>
        /// Recalculates cart item price based on current booth pricing
        /// Returns true if price was updated (difference > 0.01), false otherwise
        /// </summary>
        public async Task<bool> RecalculateCartItemPriceAsync(CartItem item)
        {
            // Get booth with current pricing
            var booth = await _boothRepository.GetAsync(item.BoothId, includeDetails: true);

            // Calculate new total price
            var days = item.GetDaysCount();
            var newTotalPrice = CalculateTotalPrice(booth, days);
            var newPricePerDay = newTotalPrice / days;

            // Check if price changed significantly (more than 0.01)
            var currentPrice = item.GetTotalPrice();
            var priceDifference = Math.Abs(newTotalPrice - currentPrice);

            if (priceDifference > 0.01m)
            {
                // Update price and track the change
                item.UpdatePrice(newTotalPrice, newPricePerDay);
                return true;
            }

            return false;
        }

        private async Task<Currency> GetTenantCurrencyAsync()
        {
            var currencySetting = await _settingProvider.GetOrNullAsync(MPSettings.Tenant.Currency);
            if (int.TryParse(currencySetting, out var currencyValue))
            {
                return (Currency)currencyValue;
            }

            // Default to PLN if setting not found
            return Currency.PLN;
        }
    }
}