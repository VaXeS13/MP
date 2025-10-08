using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MP.Domain.Payments
{
    public interface IPrzelewy24Service
    {
        Task<Przelewy24PaymentResult> CreatePaymentAsync(Przelewy24PaymentRequest request);
        Task<Przelewy24PaymentStatus> GetPaymentStatusAsync(string transactionId);
        Task<bool> VerifyPaymentAsync(string transactionId, decimal amount);
        Task<List<Przelewy24PaymentMethod>> GetPaymentMethodsAsync(string currency = "PLN");
        string GeneratePaymentUrl(string transactionId);
    }

    public class Przelewy24PaymentRequest
    {
        public string MerchantId { get; set; } = null!;
        public string PosId { get; set; } = null!;
        public string SessionId { get; set; } = null!;
        public decimal Amount { get; set; } // kwota w groszach
        public string Currency { get; set; } = "PLN";
        public string Description { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string ClientName { get; set; } = null!;
        public string Country { get; set; } = "PL";
        public string Language { get; set; } = "pl";
        public string UrlReturn { get; set; } = null!;
        public string UrlStatus { get; set; } = null!;
    }

    public class Przelewy24PaymentResult
    {
        public string TransactionId { get; set; } = null!;
        public string PaymentUrl { get; set; } = null!;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class Przelewy24PaymentStatus
    {
        public string TransactionId { get; set; } = null!;
        public string Status { get; set; } = null!; // "pending", "completed", "failed", "cancelled"
        public decimal? Amount { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class Przelewy24PaymentMethod
    {
        public int Id { get; set; }
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
    }
}