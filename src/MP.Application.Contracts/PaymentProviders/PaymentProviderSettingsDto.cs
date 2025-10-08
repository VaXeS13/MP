using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MP.Application.Contracts.PaymentProviders
{
    public class PaymentProviderSettingsDto
    {
        public Przelewy24SettingsDto Przelewy24 { get; set; } = new();
        public PayPalSettingsDto PayPal { get; set; } = new();
        public StripeSettingsDto Stripe { get; set; } = new();
    }

    public class Przelewy24SettingsDto : IValidatableObject
    {
        public bool Enabled { get; set; }

        [StringLength(50)]
        public string? MerchantId { get; set; }

        [StringLength(50)]
        public string? PosId { get; set; }

        [StringLength(100)]
        public string? ApiKey { get; set; }

        [StringLength(100)]
        public string? CrcKey { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Enabled)
            {
                if (string.IsNullOrWhiteSpace(MerchantId))
                    yield return new ValidationResult("The MerchantId field is required when Przelewy24 is enabled.", new[] { nameof(MerchantId) });

                if (string.IsNullOrWhiteSpace(PosId))
                    yield return new ValidationResult("The PosId field is required when Przelewy24 is enabled.", new[] { nameof(PosId) });

                if (string.IsNullOrWhiteSpace(ApiKey))
                    yield return new ValidationResult("The ApiKey field is required when Przelewy24 is enabled.", new[] { nameof(ApiKey) });

                if (string.IsNullOrWhiteSpace(CrcKey))
                    yield return new ValidationResult("The CrcKey field is required when Przelewy24 is enabled.", new[] { nameof(CrcKey) });
            }
        }
    }

    public class PayPalSettingsDto : IValidatableObject
    {
        public bool Enabled { get; set; }

        [StringLength(100)]
        public string? ClientId { get; set; }

        [StringLength(100)]
        public string? ClientSecret { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Enabled)
            {
                if (string.IsNullOrWhiteSpace(ClientId))
                    yield return new ValidationResult("The ClientId field is required when PayPal is enabled.", new[] { nameof(ClientId) });

                if (string.IsNullOrWhiteSpace(ClientSecret))
                    yield return new ValidationResult("The ClientSecret field is required when PayPal is enabled.", new[] { nameof(ClientSecret) });
            }
        }
    }

    public class StripeSettingsDto : IValidatableObject
    {
        public bool Enabled { get; set; }

        [StringLength(200)]
        public string? PublishableKey { get; set; }

        [StringLength(200)]
        public string? SecretKey { get; set; }

        [StringLength(200)]
        public string? WebhookSecret { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Enabled)
            {
                if (string.IsNullOrWhiteSpace(PublishableKey))
                    yield return new ValidationResult("The PublishableKey field is required when Stripe is enabled.", new[] { nameof(PublishableKey) });

                if (string.IsNullOrWhiteSpace(SecretKey))
                    yield return new ValidationResult("The SecretKey field is required when Stripe is enabled.", new[] { nameof(SecretKey) });
            }
        }
    }

    public class UpdatePaymentProviderSettingsDto
    {
        public Przelewy24SettingsDto Przelewy24 { get; set; } = new();
        public PayPalSettingsDto PayPal { get; set; } = new();
        public StripeSettingsDto Stripe { get; set; } = new();
    }
}