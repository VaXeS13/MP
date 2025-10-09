using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.Settings;
using Volo.Abp.SettingManagement;
using MP.Application.Contracts.PaymentProviders;
using MP.Domain.Settings;
using MP.Permissions;

namespace MP.Application.PaymentProviders
{
    [RemoteService]
    [Authorize(MPPermissions.PaymentProviders.Manage)]
    public class PaymentProviderSettingsAppService : ApplicationService, IPaymentProviderSettingsAppService
    {
        private readonly ISettingManager _settingManager;
        private readonly ISettingProvider _settingProvider;
        private readonly IDistributedCache<PaymentProviderSettingsDto> _cache;

        public PaymentProviderSettingsAppService(
            ISettingManager settingManager,
            ISettingProvider settingProvider,
            IDistributedCache<PaymentProviderSettingsDto> cache)
        {
            _settingManager = settingManager;
            _settingProvider = settingProvider;
            _cache = cache;
        }

        public async Task<PaymentProviderSettingsDto> GetAsync()
        {
            var cacheKey = $"PaymentSettings_Tenant_{CurrentTenant?.Id}";

            var cachedData = await _cache.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    var settings = new PaymentProviderSettingsDto();

                    // Load Przelewy24 settings
                    settings.Przelewy24.Enabled = await _settingProvider.GetAsync<bool>(MPSettings.PaymentProviders.Przelewy24Enabled);
                    settings.Przelewy24.MerchantId = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.Przelewy24MerchantId);
                    settings.Przelewy24.PosId = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.Przelewy24PosId);
                    settings.Przelewy24.ApiKey = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.Przelewy24ApiKey);
                    settings.Przelewy24.CrcKey = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.Przelewy24CrcKey);

                    // Load PayPal settings
                    settings.PayPal.Enabled = await _settingProvider.GetAsync<bool>(MPSettings.PaymentProviders.PayPalEnabled);
                    settings.PayPal.ClientId = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.PayPalClientId);
                    settings.PayPal.ClientSecret = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.PayPalClientSecret);

                    // Load Stripe settings
                    settings.Stripe.Enabled = await _settingProvider.GetAsync<bool>(MPSettings.PaymentProviders.StripeEnabled);
                    settings.Stripe.PublishableKey = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.StripePublishableKey);
                    settings.Stripe.SecretKey = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.StripeSecretKey);
                    settings.Stripe.WebhookSecret = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.StripeWebhookSecret);

                    return settings;
                },
                () => new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                }
            );

            return cachedData;
        }

        public async Task UpdateAsync(UpdatePaymentProviderSettingsDto input)
        {
            // Update Przelewy24 settings
            await _settingManager.SetForCurrentTenantAsync(MPSettings.PaymentProviders.Przelewy24Enabled, input.Przelewy24.Enabled.ToString().ToLowerInvariant());
            await _settingManager.SetForCurrentTenantAsync(MPSettings.PaymentProviders.Przelewy24MerchantId, input.Przelewy24.MerchantId ?? "");
            await _settingManager.SetForCurrentTenantAsync(MPSettings.PaymentProviders.Przelewy24PosId, input.Przelewy24.PosId ?? "");
            await _settingManager.SetForCurrentTenantAsync(MPSettings.PaymentProviders.Przelewy24ApiKey, input.Przelewy24.ApiKey ?? "");
            await _settingManager.SetForCurrentTenantAsync(MPSettings.PaymentProviders.Przelewy24CrcKey, input.Przelewy24.CrcKey ?? "");

            // Update PayPal settings
            await _settingManager.SetForCurrentTenantAsync(MPSettings.PaymentProviders.PayPalEnabled, input.PayPal.Enabled.ToString().ToLowerInvariant());
            await _settingManager.SetForCurrentTenantAsync(MPSettings.PaymentProviders.PayPalClientId, input.PayPal.ClientId ?? "");
            await _settingManager.SetForCurrentTenantAsync(MPSettings.PaymentProviders.PayPalClientSecret, input.PayPal.ClientSecret ?? "");

            // Update Stripe settings
            await _settingManager.SetForCurrentTenantAsync(MPSettings.PaymentProviders.StripeEnabled, input.Stripe.Enabled.ToString().ToLowerInvariant());
            await _settingManager.SetForCurrentTenantAsync(MPSettings.PaymentProviders.StripePublishableKey, input.Stripe.PublishableKey ?? "");
            await _settingManager.SetForCurrentTenantAsync(MPSettings.PaymentProviders.StripeSecretKey, input.Stripe.SecretKey ?? "");
            await _settingManager.SetForCurrentTenantAsync(MPSettings.PaymentProviders.StripeWebhookSecret, input.Stripe.WebhookSecret ?? "");

            // Invalidate cache
            await InvalidateCacheAsync();
        }

        private async Task InvalidateCacheAsync()
        {
            var cacheKey = $"PaymentSettings_Tenant_{CurrentTenant?.Id}";
            await _cache.RemoveAsync(cacheKey);
        }
    }
}