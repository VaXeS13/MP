using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;
using MP.Application.Contracts.Booths;

namespace MP.Carts
{
    public class CartItemDto : EntityDto<Guid>
    {
        public Guid CartId { get; set; }
        public Guid BoothId { get; set; }
        public Guid BoothTypeId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public decimal PricePerDay { get; set; }
        public string? Notes { get; set; }

        // Calculated fields
        public int DaysCount { get; set; }
        public decimal TotalPrice { get; set; }

        // Promotion discount applied to this item
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal FinalPrice { get; set; }

        // Related entity data for display
        public string? BoothNumber { get; set; }
        public string? BoothDescription { get; set; }
        public string? BoothTypeName { get; set; }
        public string? Currency { get; set; }

        // Pricing periods for detailed price breakdown display
        public List<BoothPricingPeriodDto>? PricingPeriods { get; set; }

        // Reservation expiration
        public DateTime? ReservationExpiresAt { get; set; }

        // Deprecated: Use ReservationExpiresAt instead
        public bool IsExpired { get; set; }

        // Price update tracking
        public decimal? OldStoredTotalPrice { get; set; }
        public bool PriceWasUpdated { get; set; }
    }
}