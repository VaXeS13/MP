using System.Collections.Generic;

namespace MP.Application.Contracts.Payments
{
    public class PaymentProviderDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public List<string> SupportedCurrencies { get; set; } = new();
        public bool IsActive { get; set; }
    }

    public class PaymentMethodDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public string? ProcessingTime { get; set; }
        public PaymentMethodFeesDto? Fees { get; set; }
        public bool IsActive { get; set; }
    }

    public class PaymentMethodFeesDto
    {
        public decimal? FixedAmount { get; set; }
        public decimal? PercentageAmount { get; set; }
        public string? Description { get; set; }
    }

    public class CreatePaymentRequestDto
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "PLN";
        public string Description { get; set; } = null!;
        public string ProviderId { get; set; } = null!;
        public string? MethodId { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class PaymentCreationResultDto
    {
        public bool Success { get; set; }
        public string? PaymentUrl { get; set; }
        public string? TransactionId { get; set; }
        public string? ErrorMessage { get; set; }
    }
}