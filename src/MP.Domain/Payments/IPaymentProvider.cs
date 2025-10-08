using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MP.Domain.Payments
{
    /// <summary>
    /// Generic interface for payment providers (Przelewy24, Stripe, PayPal, etc.)
    /// </summary>
    public interface IPaymentProvider
    {
        /// <summary>
        /// Unique identifier for the provider (e.g., "przelewy24", "stripe", "paypal")
        /// </summary>
        string ProviderId { get; }

        /// <summary>
        /// Display name for the provider
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Provider description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Provider logo URL
        /// </summary>
        string? LogoUrl { get; }

        /// <summary>
        /// Supported currencies by this provider
        /// </summary>
        List<string> SupportedCurrencies { get; }

        /// <summary>
        /// Whether this provider is currently active and available
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Create a payment transaction
        /// </summary>
        Task<PaymentResult> CreatePaymentAsync(PaymentRequest request);

        /// <summary>
        /// Get payment status
        /// </summary>
        Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionId);

        /// <summary>
        /// Verify payment (usually called after webhook notification)
        /// </summary>
        Task<bool> VerifyPaymentAsync(string transactionId, decimal amount);

        /// <summary>
        /// Get available payment methods for this provider
        /// </summary>
        Task<List<PaymentMethod>> GetPaymentMethodsAsync(string currency = "PLN");

        /// <summary>
        /// Generate payment URL for redirect
        /// </summary>
        string GeneratePaymentUrl(string transactionId);
    }

    /// <summary>
    /// Generic payment request
    /// </summary>
    public class PaymentRequest
    {
        public string MerchantId { get; set; } = null!;
        public string SessionId { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "PLN";
        public string Description { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string ClientName { get; set; } = null!;
        public string Country { get; set; } = "PL";
        public string Language { get; set; } = "pl";
        public string UrlReturn { get; set; } = null!;
        public string UrlStatus { get; set; } = null!;
        public string? MethodId { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Generic payment result
    /// </summary>
    public class PaymentResult
    {
        public string TransactionId { get; set; } = null!;
        public string PaymentUrl { get; set; } = null!;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> ProviderData { get; set; } = new();
    }

    /// <summary>
    /// Generic payment status result
    /// </summary>
    public class PaymentStatusResult
    {
        public string TransactionId { get; set; } = null!;
        public string Status { get; set; } = null!; // "pending", "completed", "failed", "cancelled"
        public decimal? Amount { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> ProviderData { get; set; } = new();
    }

    /// <summary>
    /// Generic payment method
    /// </summary>
    public class PaymentMethod
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public bool IsActive { get; set; }
        public bool IsAvailable { get; set; }
        public List<string> SupportedCurrencies { get; set; } = new();
        public string? ProcessingTime { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public PaymentMethodType Type { get; set; }
        public Dictionary<string, object> ProviderData { get; set; } = new();
    }

    /// <summary>
    /// Payment method types
    /// </summary>
    public enum PaymentMethodType
    {
        BankTransfer = 0,
        CreditCard = 1,
        DebitCard = 2,
        DigitalWallet = 3,
        Cryptocurrency = 4,
        BLIK = 5,
        PayByLink = 6,
        Other = 99
    }
}