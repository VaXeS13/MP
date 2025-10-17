using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using MP.Carts;
using MP.Domain.Booths;

namespace MP.Domain.Carts
{
    public class CartItem : FullAuditedEntity<Guid>
    {
        public Guid CartId { get; private set; }
        public Guid BoothId { get; private set; }
        public Guid BoothTypeId { get; private set; }

        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }

        public decimal PricePerDay { get; private set; }
        public Currency Currency { get; private set; }
        public string? Notes { get; private set; }

        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercentage { get; set; }

        public CartItemType ItemType { get; private set; }
        public Guid? ExtendedRentalId { get; private set; }
        public Guid? RentalId { get; private set; } // Rental created by admin for online payment

        /// <summary>
        /// Reservation expiration time for this cart item
        /// When user adds item to cart, booth is reserved for a limited time (e.g., 5 minutes)
        /// When admin creates rental with online payment, timeout can be longer (e.g., 30 minutes)
        /// </summary>
        public DateTime? ReservationExpiresAt { get; private set; }

        // Navigation property
        public Cart Cart { get; set; } = null!;

        private CartItem() { }

        public CartItem(
            Guid id,
            Guid cartId,
            Guid boothId,
            Guid boothTypeId,
            DateTime startDate,
            DateTime endDate,
            decimal pricePerDay,
            Currency currency,
            CartItemType itemType = CartItemType.Rental,
            Guid? extendedRentalId = null,
            Guid? rentalId = null,
            DateTime? reservationExpiresAt = null,
            string? notes = null) : base(id)
        {
            CartId = cartId;
            BoothId = boothId;
            BoothTypeId = boothTypeId;
            SetPeriod(startDate, endDate);
            SetPricePerDay(pricePerDay);
            Currency = currency;
            ItemType = itemType;
            ExtendedRentalId = extendedRentalId;
            RentalId = rentalId;
            ReservationExpiresAt = reservationExpiresAt;
            Notes = notes?.Trim();
        }

        public void SetReservationExpiration(DateTime? expiresAt)
        {
            ReservationExpiresAt = expiresAt;
        }

        public bool IsReservationExpired()
        {
            return ReservationExpiresAt.HasValue && ReservationExpiresAt.Value < DateTime.Now;
        }

        public bool HasActiveReservation()
        {
            return ReservationExpiresAt.HasValue && ReservationExpiresAt.Value >= DateTime.Now;
        }

        /// <summary>
        /// Releases the reservation without removing the cart item
        /// Called when reservation expires - keeps item in cart but removes booth blocking
        /// Removes RentalId (Draft Rental will be deleted) but keeps ReservationExpiresAt for historical tracking
        /// </summary>
        public void ReleaseReservation()
        {
            // Keep ReservationExpiresAt for historical tracking (shows when it expired)
            // Remove RentalId because Draft Rental will be deleted
            RentalId = null;
        }

        public void SetPeriod(DateTime startDate, DateTime endDate)
        {
            if (startDate >= endDate)
                throw new BusinessException("CART_ITEM_INVALID_PERIOD")
                    .WithData("StartDate", startDate)
                    .WithData("EndDate", endDate);

            if (startDate < DateTime.Today)
                throw new BusinessException("CART_ITEM_START_DATE_IN_PAST");

            StartDate = startDate.Date;
            EndDate = endDate.Date;
        }

        public void SetPricePerDay(decimal pricePerDay)
        {
            if (pricePerDay <= 0)
                throw new BusinessException("CART_ITEM_INVALID_PRICE")
                    .WithData("PricePerDay", pricePerDay);

            PricePerDay = pricePerDay;
        }

        public void UpdateBoothType(Guid boothTypeId)
        {
            BoothTypeId = boothTypeId;
        }

        public void SetNotes(string? notes)
        {
            Notes = notes?.Trim();
        }

        public int GetDaysCount()
        {
            return (EndDate - StartDate).Days + 1; // Include both start and end date
        }

        public decimal GetTotalPrice()
        {
            return GetDaysCount() * PricePerDay;
        }

        public decimal GetFinalPrice()
        {
            return Math.Max(0, GetTotalPrice() - DiscountAmount);
        }

        public void ApplyDiscount(decimal discountAmount, decimal discountPercentage = 0)
        {
            if (discountAmount < 0)
                throw new BusinessException("DISCOUNT_CANNOT_BE_NEGATIVE");

            var totalPrice = GetTotalPrice();
            if (discountAmount > totalPrice)
                throw new BusinessException("DISCOUNT_CANNOT_EXCEED_ITEM_PRICE");

            DiscountAmount = discountAmount;
            DiscountPercentage = discountPercentage;
        }

        public void RemoveDiscount()
        {
            DiscountAmount = 0;
            DiscountPercentage = 0;
        }

        public bool OverlapsWith(DateTime startDate, DateTime endDate)
        {
            return StartDate <= endDate && EndDate >= startDate;
        }
    }
}