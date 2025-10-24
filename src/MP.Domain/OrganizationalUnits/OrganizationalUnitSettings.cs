using System;
using System.Collections.Generic;
using System.Text.Json;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace MP.Domain.OrganizationalUnits
{
    public class OrganizationalUnitSettings : FullAuditedEntity<Guid>
    {
        private const string DefaultCurrency = "PLN";
        private const string DefaultPaymentProviders = "{\"stripe\":true,\"p24\":true,\"paypal\":false}";

        public Guid? TenantId { get; private set; }
        public Guid OrganizationalUnitId { get; private set; }
        public string Currency { get; private set; } = DefaultCurrency;
        public string EnabledPaymentProviders { get; private set; } = DefaultPaymentProviders;
        public string? DefaultPaymentProvider { get; private set; }
        public string? LogoUrl { get; private set; }
        public string? BannerText { get; private set; }
        public bool IsMainUnit { get; private set; }

        // Navigation properties
        public virtual OrganizationalUnit? OrganizationalUnit { get; set; }

        private static readonly HashSet<string> ValidCurrencies = new()
        {
            "PLN", "EUR", "USD", "GBP", "CZK"
        };

        private static readonly HashSet<string> ValidPaymentProviders = new()
        {
            "stripe", "p24", "paypal"
        };

        // EF Core constructor
        private OrganizationalUnitSettings() { }

        public OrganizationalUnitSettings(
            Guid id,
            Guid organizationalUnitId,
            Guid? tenantId = null,
            bool isMainUnit = false
        ) : base(id)
        {
            TenantId = tenantId;
            OrganizationalUnitId = organizationalUnitId;
            Currency = DefaultCurrency;
            EnabledPaymentProviders = DefaultPaymentProviders;
            DefaultPaymentProvider = "stripe";
            IsMainUnit = isMainUnit;
        }

        public void UpdateCurrency(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                throw new BusinessException("UnitSettings.CurrencyRequired", "Currency is required");

            if (!ValidCurrencies.Contains(currency.ToUpper()))
                throw new BusinessException("UnitSettings.CurrencyInvalid", "Invalid currency. Allowed: PLN, EUR, USD, GBP, CZK");

            Currency = currency.ToUpper();
        }

        public void UpdatePaymentProviders(Dictionary<string, bool> providers)
        {
            if (providers == null || providers.Count == 0)
                throw new BusinessException("UnitSettings.ProvidersRequired", "At least one payment provider must be enabled");

            // Validate all provider keys
            foreach (var key in providers.Keys)
            {
                if (!ValidPaymentProviders.Contains(key.ToLower()))
                    throw new BusinessException("UnitSettings.ProviderInvalid", $"Invalid payment provider: {key}");
            }

            // Check if at least one provider is enabled
            var anyEnabled = false;
            foreach (var value in providers.Values)
            {
                if (value)
                {
                    anyEnabled = true;
                    break;
                }
            }

            if (!anyEnabled)
                throw new BusinessException("UnitSettings.NoProvidersEnabled", "At least one payment provider must be enabled");

            // Serialize to JSON
            var json = JsonSerializer.Serialize(providers);
            EnabledPaymentProviders = json;

            // If default provider is not enabled, reset it
            if (!string.IsNullOrEmpty(DefaultPaymentProvider))
            {
                var defaultKey = DefaultPaymentProvider.ToLower();
                if (!providers.ContainsKey(defaultKey) || !providers[defaultKey])
                {
                    DefaultPaymentProvider = null;
                }
            }
        }

        public void SetDefaultPaymentProvider(string? providerName)
        {
            if (string.IsNullOrEmpty(providerName))
            {
                DefaultPaymentProvider = null;
                return;
            }

            if (!ValidPaymentProviders.Contains(providerName.ToLower()))
                throw new BusinessException("UnitSettings.ProviderInvalid", $"Invalid payment provider: {providerName}");

            // Check if provider is enabled
            try
            {
                var providers = JsonSerializer.Deserialize<Dictionary<string, bool>>(EnabledPaymentProviders);
                if (providers == null || !providers.ContainsKey(providerName.ToLower()) || !providers[providerName.ToLower()])
                    throw new BusinessException("UnitSettings.ProviderNotEnabled", $"Payment provider {providerName} is not enabled");
            }
            catch (JsonException)
            {
                throw new BusinessException("UnitSettings.ProviderConfigInvalid", "Invalid payment provider configuration");
            }

            DefaultPaymentProvider = providerName.ToLower();
        }

        public void UpdateBranding(string? logoUrl, string? bannerText)
        {
            if (!string.IsNullOrWhiteSpace(logoUrl) && logoUrl.Length > 500)
                throw new BusinessException("UnitSettings.LogoUrlTooLong", "Logo URL cannot exceed 500 characters");

            if (!string.IsNullOrWhiteSpace(bannerText) && bannerText.Length > 1000)
                throw new BusinessException("UnitSettings.BannerTextTooLong", "Banner text cannot exceed 1000 characters");

            LogoUrl = logoUrl;
            BannerText = bannerText;
        }

        public bool IsPaymentProviderEnabled(string providerName)
        {
            try
            {
                var providers = JsonSerializer.Deserialize<Dictionary<string, bool>>(EnabledPaymentProviders);
                if (providers == null)
                    return false;

                var key = providerName.ToLower();
                return providers.ContainsKey(key) && providers[key];
            }
            catch
            {
                return false;
            }
        }

        public Dictionary<string, bool>? GetPaymentProviders()
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, bool>>(EnabledPaymentProviders);
            }
            catch
            {
                return null;
            }
        }
    }
}
