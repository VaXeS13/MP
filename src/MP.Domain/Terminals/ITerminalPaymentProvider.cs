using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MP.Domain.Terminals
{
    /// <summary>
    /// Interface for payment terminal providers (Ingenico, Verifone, Stripe Terminal, SumUp, Adyen, etc.)
    /// </summary>
    public interface ITerminalPaymentProvider
    {
        /// <summary>
        /// Unique identifier for the terminal provider (e.g., "ingenico", "verifone", "stripe_terminal", "sumup", "adyen", "mock")
        /// </summary>
        string ProviderId { get; }

        /// <summary>
        /// Display name for the terminal provider
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Provider description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Initialize the terminal with configuration
        /// </summary>
        Task InitializeAsync(TenantTerminalSettings settings);

        /// <summary>
        /// Authorize a payment on the terminal (for card payments)
        /// Returns transaction ID and status
        /// </summary>
        Task<TerminalPaymentResult> AuthorizePaymentAsync(TerminalPaymentRequest request);

        /// <summary>
        /// Capture a previously authorized payment
        /// </summary>
        Task<TerminalPaymentResult> CapturePaymentAsync(string transactionId, decimal amount);

        /// <summary>
        /// Refund a payment
        /// </summary>
        Task<TerminalPaymentResult> RefundPaymentAsync(string transactionId, decimal amount, string? reason = null);

        /// <summary>
        /// Cancel/void a payment (before capture)
        /// </summary>
        Task<TerminalPaymentResult> CancelPaymentAsync(string transactionId);

        /// <summary>
        /// Get the status of a terminal payment
        /// </summary>
        Task<TerminalPaymentStatus> GetPaymentStatusAsync(string transactionId);

        /// <summary>
        /// Check if terminal is online and ready
        /// </summary>
        Task<bool> CheckTerminalStatusAsync();
    }

    /// <summary>
    /// Request for terminal payment
    /// </summary>
    public class TerminalPaymentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "PLN";
        public string? Description { get; set; }
        public string? ReferenceId { get; set; }
        public Guid RentalItemId { get; set; }
        public string? RentalItemName { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public Dictionary<string, string>? AdditionalData { get; set; }
    }

    /// <summary>
    /// Result of terminal payment operation
    /// </summary>
    public class TerminalPaymentResult
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public string? AuthorizationCode { get; set; }
        public string Status { get; set; } = null!; // "authorized", "captured", "declined", "error", "cancelled"
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? Timestamp { get; set; }
        public string? CardType { get; set; }
        public string? LastFourDigits { get; set; }
        public string? RawResponse { get; set; }
        public Dictionary<string, object> ProviderData { get; set; } = new();
    }

    /// <summary>
    /// Status of a terminal payment
    /// </summary>
    public class TerminalPaymentStatus
    {
        public string TransactionId { get; set; } = null!;
        public string Status { get; set; } = null!; // "pending", "authorized", "captured", "declined", "refunded", "cancelled", "error"
        public decimal? Amount { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> ProviderData { get; set; } = new();
    }
}