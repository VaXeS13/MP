using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using MP.Domain.Terminals;

namespace MP.Application.Terminals
{
    public interface ITerminalPaymentProviderFactory
    {
        Task<ITerminalPaymentProvider?> GetProviderAsync(string providerId, Guid? tenantId);
        Task<ITerminalPaymentProvider?> GetActiveProviderAsync(Guid? tenantId);
        Task<List<ITerminalPaymentProvider>> GetAvailableProvidersAsync(Guid? tenantId);
        List<ITerminalPaymentProvider> GetAllProviders();
        Task<TenantTerminalSettings?> GetTerminalSettingsAsync(Guid? tenantId);
        Task<TenantTerminalSettings?> GetActiveTerminalSettingsAsync(Guid? tenantId);
        Task<List<TenantTerminalSettings>> GetAllTerminalSettingsAsync(Guid? tenantId);
    }

    public class TerminalPaymentProviderFactory : ITerminalPaymentProviderFactory, ITransientDependency
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IRepository<TenantTerminalSettings, Guid> _settingsRepository;
        private readonly ILogger<TerminalPaymentProviderFactory> _logger;
        private readonly List<ITerminalPaymentProvider> _allProviders;

        public TerminalPaymentProviderFactory(
            IServiceProvider serviceProvider,
            IRepository<TenantTerminalSettings, Guid> settingsRepository,
            ILogger<TerminalPaymentProviderFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _settingsRepository = settingsRepository;
            _logger = logger;

            // Register all known terminal providers
            _allProviders = new List<ITerminalPaymentProvider>
            {
                _serviceProvider.GetRequiredService<MockTerminalProvider>(),
                _serviceProvider.GetRequiredService<Providers.IngenicoLane5000Provider>(),
                _serviceProvider.GetRequiredService<Providers.VerifoneVX520Provider>(),
                _serviceProvider.GetRequiredService<Providers.NetsTerminalProvider>(),
                _serviceProvider.GetRequiredService<Providers.SquareTerminalProvider>(),
                _serviceProvider.GetRequiredService<Providers.StripeTerminalProvider>(),
                _serviceProvider.GetRequiredService<Providers.SumUpProvider>(),
                _serviceProvider.GetRequiredService<Providers.AdyenProvider>()
                // TODO: Add other providers when implemented:
                // _serviceProvider.GetRequiredService<Providers.PAXA920Provider>()
            };

            _logger.LogInformation(
                "TerminalPaymentProviderFactory: Initialized with {Count} providers: {Providers}",
                _allProviders.Count,
                string.Join(", ", _allProviders.Select(p => p.ProviderId))
            );
        }

        public async Task<ITerminalPaymentProvider?> GetProviderAsync(string providerId, Guid? tenantId)
        {
            try
            {
                var provider = _allProviders.FirstOrDefault(p =>
                    p.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase)
                );

                if (provider == null)
                {
                    _logger.LogWarning("Terminal provider {ProviderId} not found", providerId);
                    return null;
                }

                // Get tenant-specific settings
                var settings = await GetTerminalSettingsAsync(tenantId);

                if (settings == null || !settings.IsEnabled)
                {
                    _logger.LogWarning(
                        "Terminal provider {ProviderId} is not configured or disabled for tenant {TenantId}",
                        providerId, tenantId);
                    return null;
                }

                // Initialize provider with tenant settings
                await provider.InitializeAsync(settings);

                return provider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting terminal provider {ProviderId} for tenant {TenantId}",
                    providerId, tenantId);
                return null;
            }
        }

        public async Task<List<ITerminalPaymentProvider>> GetAvailableProvidersAsync(Guid? tenantId)
        {
            try
            {
                var settings = await GetTerminalSettingsAsync(tenantId);

                if (settings == null || !settings.IsEnabled)
                {
                    _logger.LogWarning("No terminal settings configured for tenant {TenantId}", tenantId);
                    return new List<ITerminalPaymentProvider>();
                }

                var provider = _allProviders.FirstOrDefault(p =>
                    p.ProviderId.Equals(settings.ProviderId, StringComparison.OrdinalIgnoreCase)
                );

                if (provider == null)
                {
                    _logger.LogWarning("Configured provider {ProviderId} not found for tenant {TenantId}",
                        settings.ProviderId, tenantId);
                    return new List<ITerminalPaymentProvider>();
                }

                await provider.InitializeAsync(settings);

                return new List<ITerminalPaymentProvider> { provider };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available terminal providers for tenant {TenantId}", tenantId);
                return new List<ITerminalPaymentProvider>();
            }
        }

        public List<ITerminalPaymentProvider> GetAllProviders() => _allProviders.ToList();

        public async Task<ITerminalPaymentProvider?> GetActiveProviderAsync(Guid? tenantId)
        {
            try
            {
                var settings = await GetActiveTerminalSettingsAsync(tenantId);

                if (settings == null)
                {
                    _logger.LogWarning("No active terminal configured for tenant {TenantId}", tenantId);
                    return null;
                }

                return await GetProviderAsync(settings.ProviderId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active terminal provider for tenant {TenantId}", tenantId);
                return null;
            }
        }

        public async Task<TenantTerminalSettings?> GetTerminalSettingsAsync(Guid? tenantId)
        {
            try
            {
                var queryable = await _settingsRepository.GetQueryableAsync();
                var settings = queryable
                    .Where(s => s.TenantId == tenantId && s.IsEnabled)
                    .FirstOrDefault();

                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting terminal settings for tenant {TenantId}", tenantId);
                return null;
            }
        }

        public async Task<TenantTerminalSettings?> GetActiveTerminalSettingsAsync(Guid? tenantId)
        {
            try
            {
                var queryable = await _settingsRepository.GetQueryableAsync();
                var settings = queryable
                    .Where(s => s.TenantId == tenantId && s.IsEnabled && s.IsActive)
                    .FirstOrDefault();

                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active terminal settings for tenant {TenantId}", tenantId);
                return null;
            }
        }

        public async Task<List<TenantTerminalSettings>> GetAllTerminalSettingsAsync(Guid? tenantId)
        {
            try
            {
                var queryable = await _settingsRepository.GetQueryableAsync();
                var settings = queryable
                    .Where(s => s.TenantId == tenantId)
                    .ToList();

                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all terminal settings for tenant {TenantId}", tenantId);
                return new List<TenantTerminalSettings>();
            }
        }
    }
}