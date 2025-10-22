using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;
using MP.Rentals;
using MP.Application.Contracts.Rentals;

namespace MP.Application.Contracts.CustomerDashboard
{
    /// <summary>
    /// Detailed rental information for customer
    /// </summary>
    public class MyRentalDetailDto : FullAuditedEntityDto<Guid>
    {
        public Guid BoothId { get; set; }
        public string BoothNumber { get; set; } = null!;
        public string BoothTypeName { get; set; } = null!;
        public decimal BoothPricePerDay { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalDays { get; set; }
        public int DaysRemaining { get; set; }
        public int DaysElapsed { get; set; }

        public RentalStatus Status { get; set; }
        public string StatusDisplayName { get; set; } = null!;

        public decimal TotalCost { get; set; }
        public decimal PaidAmount { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaidDate { get; set; }

        /// <summary>
        /// Price breakdown showing how total price was calculated from multi-period pricing
        /// </summary>
        public PriceBreakdownDto? PriceBreakdown { get; set; }

        // Promotion details
        public Guid? AppliedPromotionId { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal OriginalAmount { get; set; }
        public string? PromoCodeUsed { get; set; }

        public string? Notes { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Items summary
        public int TotalItems { get; set; }
        public int SoldItems { get; set; }
        public int AvailableItems { get; set; }
        public int ReclaimedItems { get; set; }

        // Sales summary
        public decimal TotalSalesAmount { get; set; }
        public decimal TotalCommissionPaid { get; set; }
        public decimal NetEarnings { get; set; }

        // Actions
        public bool CanExtend { get; set; }
        public bool CanCancel { get; set; }
        public bool IsExpiringSoon { get; set; }
        public bool IsOverdue { get; set; }

        // QR Code
        public string? QRCodeUrl { get; set; }

        // Extension options
        public List<ExtensionOptionDto> ExtensionOptions { get; set; } = new();

        // Recent activity
        public List<RentalActivityDto> RecentActivity { get; set; } = new();
    }

    /// <summary>
    /// Extension option for rental
    /// </summary>
    public class ExtensionOptionDto
    {
        public int Days { get; set; }
        public string DisplayName { get; set; } = null!;
        public decimal Cost { get; set; }
        public DateTime NewEndDate { get; set; }
    }

    /// <summary>
    /// Rental activity log
    /// </summary>
    public class RentalActivityDto
    {
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? ItemName { get; set; }
        public decimal? Amount { get; set; }
    }

    /// <summary>
    /// Request rental extension
    /// </summary>
    public class RequestRentalExtensionDto
    {
        [Required]
        public Guid RentalId { get; set; }

        [Required]
        [Range(1, 365)]
        public int ExtensionDays { get; set; }

        public string? PaymentProviderId { get; set; }
    }

    /// <summary>
    /// Extension result
    /// </summary>
    public class RentalExtensionResultDto
    {
        public Guid RentalId { get; set; }
        public DateTime NewEndDate { get; set; }
        public decimal AdditionalCost { get; set; }
        public bool PaymentRequired { get; set; }
        public string? PaymentUrl { get; set; }
        public string? PaymentSessionId { get; set; }
    }

    /// <summary>
    /// Rental calendar DTO
    /// </summary>
    public class MyRentalCalendarDto
    {
        public List<RentalCalendarEventDto> Events { get; set; } = new();
        public List<DateTime> ImportantDates { get; set; } = new();
    }

    /// <summary>
    /// Calendar event
    /// </summary>
    public class RentalCalendarEventDto
    {
        public Guid RentalId { get; set; }
        public string BoothNumber { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = null!;
        public string Color { get; set; } = null!;
        public bool IsExpiringSoon { get; set; }
    }

    /// <summary>
    /// Get customer rental history
    /// </summary>
    public class GetMyRentalsDto : PagedAndSortedResultRequestDto
    {
        public RentalStatus? Status { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public bool? IncludeCompleted { get; set; }
    }
}
