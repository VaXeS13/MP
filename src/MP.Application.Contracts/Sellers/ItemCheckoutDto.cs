using System;
using System.ComponentModel.DataAnnotations;

namespace MP.Application.Contracts.Sellers
{
    /// <summary>
    /// Request to find an item by barcode
    /// </summary>
    public class FindItemByBarcodeDto
    {
        [Required]
        [StringLength(100)]
        public string Barcode { get; set; } = null!;
    }

    /// <summary>
    /// Item details for checkout
    /// </summary>
    public class ItemForCheckoutDto
    {
        public Guid Id { get; set; }
        public Guid RentalId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Barcode { get; set; }
        public decimal? ActualPrice { get; set; }
        public decimal CommissionPercentage { get; set; }
        public string Status { get; set; } = null!;

        // Rental customer info
        public string CustomerName { get; set; } = null!;
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
    }

    /// <summary>
    /// Request to checkout an item
    /// </summary>
    public class CheckoutItemDto
    {
        [Required]
        public Guid ItemSheetItemId { get; set; }

        [Required]
        public PaymentMethodType PaymentMethod { get; set; }

        [Required]
        [Range(0.01, 1000000)]
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// Result of checkout operation
    /// </summary>
    public class CheckoutResultDto
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public string? ErrorMessage { get; set; }
        public PaymentMethodType PaymentMethod { get; set; }
        public decimal Amount { get; set; }
        public DateTime ProcessedAt { get; set; }
    }

    /// <summary>
    /// Payment method type for seller checkout
    /// </summary>
    public enum PaymentMethodType
    {
        Cash = 0,
        Card = 1
    }

    /// <summary>
    /// Available payment methods for current tenant
    /// </summary>
    public class AvailablePaymentMethodsDto
    {
        public bool CashEnabled { get; set; } = true;
        public bool CardEnabled { get; set; }
        public string? TerminalProviderId { get; set; }
        public string? TerminalProviderName { get; set; }
    }
}