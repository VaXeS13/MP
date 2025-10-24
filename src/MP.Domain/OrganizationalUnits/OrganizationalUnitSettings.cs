using System;
using System.Collections.Generic;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.OrganizationalUnits
{
    /// <summary>
    /// Stores configuration and settings specific to an organizational unit.
    /// Each unit can have its own currency, payment provider configuration, and branding.
    /// </summary>
    public class OrganizationalUnitSettings : FullAuditedEntity<Guid>, IMultiTenant
    {
        /// <summary>
        /// The tenant ID this settings entity belongs to.
        /// </summary>
        public Guid? TenantId { get; private set; }

        /// <summary>
        /// The organizational unit ID this settings apply to.
        /// One-to-one relationship with OrganizationalUnit.
        /// </summary>
        public Guid OrganizationalUnitId { get; private set; }

        /// <summary>
        /// The primary currency for this unit (e.g., PLN, EUR, USD, GBP, CZK).
        /// This overrides the tenant-level currency for this specific unit.
        /// </summary>
        public string Currency { get; private set; } = "PLN";

        /// <summary>
        /// JSON-encoded configuration of enabled payment providers for this unit.
        /// Format: {"stripe": true, "p24": true, "paypal": false}
        /// </summary>
        public string EnabledPaymentProviders { get; private set; } = "{}";

        /// <summary>
        /// The default payment provider for this unit (stripe, p24, paypal).
        /// Must be one of the enabled providers.
        /// </summary>
        public string? DefaultPaymentProvider { get; private set; }

        /// <summary>
        /// Optional logo URL for this unit's branding.
        /// </summary>
        public string? LogoUrl { get; private set; }

        /// <summary>
        /// Optional banner text to display on the unit's pages.
        /// </summary>
        public string? BannerText { get; private set; }

        /// <summary>
        /// Whether this is the default/main unit for the tenant.
        /// Used during migration to identify the primary unit.
        /// </summary>
        public bool IsMainUnit { get; private set; }

        /// <summary>
        /// Navigation property to the organizational unit.
        /// </summary>
        public virtual OrganizationalUnit OrganizationalUnit { get; set; } = null!;

        // Valid currencies
        private static readonly HashSet<string> ValidCurrencies = new()
        {
            "PLN", "EUR", "USD", "GBP", "CZK"
        };

        /// <summary>
        /// Private constructor for EF Core.
        /// </summary>
        private OrganizationalUnitSettings()
        {
        }

        /// <summary>
        /// Creates default settings for an organizational unit.
        /// </summary>
        /// <param name="id">Unique identifier for the settings.</param>
        /// <param name="organizationalUnitId">The unit these settings belong to.</param>
        /// <param name="tenantId">Optional tenant ID.</param>
        public OrganizationalUnitSettings(
            Guid id,
            Guid organizationalUnitId,
            Guid? tenantId = null) : base(id)
        {
            if (organizationalUnitId == Guid.Empty)
                throw new ArgumentException("Organizational unit ID cannot be empty.", nameof(organizationalUnitId));

            TenantId = tenantId;
            OrganizationalUnitId = organizationalUnitId;
            Currency = "PLN";
            EnabledPaymentProviders = "{}";
            IsMainUnit = false;
        }

        /// <summary>
        /// Updates the currency for this unit.
        /// </summary>
        /// <param name="currency">The currency code (e.g., PLN, EUR, USD).</param>
        public void UpdateCurrency(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                throw new BusinessException("UNIT_SETTINGS_CURRENCY_REQUIRED");

            currency = currency.ToUpper().Trim();

            if (!ValidCurrencies.Contains(currency))
                throw new BusinessException("UNIT_SETTINGS_CURRENCY_INVALID")
                    .WithData("currency", currency)
                    .WithData("validCurrencies", string.Join(", ", ValidCurrencies));

            Currency = currency;
        }

        /// <summary>
        /// Updates the enabled payment providers configuration.
        /// </summary>
        /// <param name="providers">Dictionary of provider names to enabled status.</param>
        public void UpdatePaymentProviders(Dictionary<string, bool> providers)
        {
            if (providers == null || providers.Count == 0)
                throw new BusinessException("UNIT_SETTINGS_PAYMENT_PROVIDERS_REQUIRED");

            // Simple JSON serialization - in production might use System.Text.Json
            var json = System.Text.Json.JsonSerializer.Serialize(providers);
            EnabledPaymentProviders = json;

            // Validate default provider is enabled if set
            if (!string.IsNullOrWhiteSpace(DefaultPaymentProvider))
            {
                if (!providers.TryGetValue(DefaultPaymentProvider, out var isEnabled) || !isEnabled)
                {
                    throw new BusinessException("UNIT_SETTINGS_DEFAULT_PROVIDER_NOT_ENABLED")
                        .WithData("provider", DefaultPaymentProvider);
                }
            }
        }

        /// <summary>
        /// Updates the branding settings for this unit.
        /// </summary>
        /// <param name="logoUrl">Optional logo URL.</param>
        /// <param name="bannerText">Optional banner text.</param>
        public void UpdateBranding(string? logoUrl, string? bannerText)
        {
            if (!string.IsNullOrWhiteSpace(logoUrl) && logoUrl.Length > 500)
                throw new BusinessException("UNIT_SETTINGS_LOGO_URL_TOO_LONG")
                    .WithData("maxLength", 500);

            if (!string.IsNullOrWhiteSpace(bannerText) && bannerText.Length > 1000)
                throw new BusinessException("UNIT_SETTINGS_BANNER_TEXT_TOO_LONG")
                    .WithData("maxLength", 1000);

            LogoUrl = logoUrl?.Trim();
            BannerText = bannerText?.Trim();
        }

        /// <summary>
        /// Sets this as the main unit for its tenant.
        /// </summary>
        public void SetAsMainUnit()
        {
            IsMainUnit = true;
        }
    }
}
