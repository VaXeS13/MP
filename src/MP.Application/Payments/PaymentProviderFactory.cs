using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Settings;
using MP.Domain.Payments;
using Volo.Abp.SettingManagement;

namespace MP.Application.Payments
{
    public interface IPaymentProviderFactory
    {
        Task<IPaymentProvider?> GetProviderAsync(string providerId);
        Task<List<IPaymentProvider>> GetAvailableProvidersAsync();
        List<IPaymentProvider> GetAllProviders();
        Task<bool> IsProviderAvailableAsync(string providerId);
    }

    public class PaymentProviderFactory : IPaymentProviderFactory, ITransientDependency
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISettingRepository _settingRepository;
        private readonly ILogger<PaymentProviderFactory> _logger;
        private readonly List<IPaymentProvider> _allProviders;

        public PaymentProviderFactory(
            IServiceProvider serviceProvider,
            ISettingRepository settingRepository,
            ILogger<PaymentProviderFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _settingRepository = settingRepository;
            _logger = logger;

            // Rejestrujesz wszystkie znane provider'y.
            // Ka¿dy musi mieæ unikalny ProviderId zgodny z prefixem w Settingach.
            _allProviders = new List<IPaymentProvider>
            {
                _serviceProvider.GetRequiredService<Przelewy24Provider>(),
                _serviceProvider.GetRequiredService<StripeProvider>(),
                _serviceProvider.GetRequiredService<PayPalProvider>()
            };

            _logger.LogInformation(
                "PaymentProviderFactory: Initialized with {Count} providers: {Providers}",
                _allProviders.Count,
                string.Join(", ", _allProviders.Select(p => p.ProviderId))
            );
        }

        public async Task<IPaymentProvider?> GetProviderAsync(string providerId)
        {
            try
            {
                // POPRAWKA: U¿ycie StringComparison.OrdinalIgnoreCase do znalezienia providera.
                var provider = _allProviders.FirstOrDefault(p =>
                    p.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase)
                );

                if (provider == null)
                {
                    _logger.LogWarning("PaymentProviderFactory: Provider {ProviderId} not found", providerId);
                    return null;
                }

                // Sprawdzamy, czy provider jest w³¹czony w ustawieniach (poprzez IsProviderAvailableAsync).
                var isEnabled = await IsProviderAvailableAsync(provider.ProviderId);
                if (!isEnabled)
                {
                    _logger.LogWarning("PaymentProviderFactory: Provider {ProviderId} is disabled in settings", provider.ProviderId);
                    return null;
                }

                return provider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PaymentProviderFactory: Error getting provider {ProviderId}", providerId);
                return null;
            }
        }

        public async Task<List<IPaymentProvider>> GetAvailableProvidersAsync()
        {
            try
            {
                var enabledProviderIds = await GetEnabledProviderIdsAsync();

                var availableProviders = _allProviders
                    .Where(p =>
                        // POPRAWKA: Sprawdzamy tylko, czy ProviderId jest w³¹czony w AbpSettings.
                        enabledProviderIds.Contains(p.ProviderId, StringComparer.OrdinalIgnoreCase)
                    )
                    .ToList();

                _logger.LogInformation(
                    "PaymentProviderFactory: Found {Count} available providers: {Providers}",
                    availableProviders.Count,
                    string.Join(", ", availableProviders.Select(p => p.ProviderId))
                );

                return availableProviders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PaymentProviderFactory: Error getting available providers");
                return new List<IPaymentProvider>();
            }
        }

        public List<IPaymentProvider> GetAllProviders() => _allProviders.ToList();

        public async Task<bool> IsProviderAvailableAsync(string providerId)
        {
            try
            {
                var enabledIds = await GetEnabledProviderIdsAsync();

                // Sprawdzamy, czy provider istnieje i czy jego ID jest na liœcie w³¹czonych.
                return _allProviders.Any(p =>
                    p.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase)
                    // USUNIÊTO WARUNEK p.IsActive, aby unikn¹æ problemów z asynchronicznoœci¹
                    && enabledIds.Contains(p.ProviderId, StringComparer.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PaymentProviderFactory: Error checking provider availability for {ProviderId}", providerId);
                return false;
            }
        }

        /// <summary>
        /// Pobiera listê identyfikatorów providerów, które maj¹ ustawienie *.Enabled = True w AbpSettings.
        /// </summary>
        private async Task<List<string>> GetEnabledProviderIdsAsync()
        {
            var settings = await _settingRepository.GetListAsync();

            var enabledProviders = settings
                .Where(s => s.Name.StartsWith("MP.PaymentProviders.", StringComparison.OrdinalIgnoreCase)
                            && s.Name.EndsWith(".Enabled", StringComparison.OrdinalIgnoreCase)
                            && string.Equals(s.Value, "True", StringComparison.OrdinalIgnoreCase))
                .Select(s =>
                {
                    var middle = s.Name.Substring("MP.PaymentProviders.".Length);
                    return middle[..middle.IndexOf(".Enabled", StringComparison.Ordinal)];
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return enabledProviders;
        }
    }
}