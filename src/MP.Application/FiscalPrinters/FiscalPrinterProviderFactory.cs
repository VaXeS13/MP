using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using MP.Domain.FiscalPrinters;
using MP.Domain.Terminals.Communication;

namespace MP.Application.FiscalPrinters
{
    public interface IFiscalPrinterProviderFactory
    {
        Task<IFiscalPrinterProvider?> GetProviderAsync(string providerId, Guid? tenantId);
        Task<IFiscalPrinterProvider?> GetActiveProviderAsync(Guid? tenantId);
        Task<List<IFiscalPrinterProvider>> GetAvailableProvidersAsync(Guid? tenantId);
        List<IFiscalPrinterProvider> GetAllProviders();
        Task<TenantFiscalPrinterSettings?> GetFiscalPrinterSettingsAsync(Guid? tenantId);
        Task<TenantFiscalPrinterSettings?> GetActiveFiscalPrinterSettingsAsync(Guid? tenantId);
        Task<List<TenantFiscalPrinterSettings>> GetAllFiscalPrinterSettingsAsync(Guid? tenantId);
    }

    public class FiscalPrinterProviderFactory : IFiscalPrinterProviderFactory, ITransientDependency
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IRepository<TenantFiscalPrinterSettings, Guid> _settingsRepository;
        private readonly ILogger<FiscalPrinterProviderFactory> _logger;
        private readonly List<IFiscalPrinterProvider> _allProviders;

        public FiscalPrinterProviderFactory(
            IServiceProvider serviceProvider,
            IRepository<TenantFiscalPrinterSettings, Guid> settingsRepository,
            ILogger<FiscalPrinterProviderFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _settingsRepository = settingsRepository;
            _logger = logger;

            // Register all known fiscal printer providers
            _allProviders = new List<IFiscalPrinterProvider>
            {
                _serviceProvider.GetRequiredService<Providers.PosnetThermalProvider>(),
                _serviceProvider.GetRequiredService<Providers.ElzabProvider>(),
                _serviceProvider.GetRequiredService<Providers.NovitusProvider>()
            };

            _logger.LogInformation(
                "FiscalPrinterProviderFactory: Initialized with {Count} providers: {Providers}",
                _allProviders.Count,
                string.Join(", ", _allProviders.Select(p => p.ProviderId))
            );
        }

        public async Task<IFiscalPrinterProvider?> GetProviderAsync(string providerId, Guid? tenantId)
        {
            try
            {
                var provider = _allProviders.FirstOrDefault(p =>
                    p.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase)
                );

                if (provider == null)
                {
                    _logger.LogWarning("Fiscal printer provider {ProviderId} not found", providerId);
                    return null;
                }

                // Get tenant-specific settings
                var settings = await GetFiscalPrinterSettingsAsync(tenantId);

                if (settings == null || !settings.IsEnabled)
                {
                    _logger.LogWarning(
                        "Fiscal printer provider {ProviderId} is not configured or disabled for tenant {TenantId}",
                        providerId, tenantId);
                    return null;
                }

                // Initialize provider with tenant settings
                var fiscalSettings = new FiscalPrinterSettings
                {
                    ProviderId = settings.ProviderId,
                    ConnectionSettings = new TerminalConnectionSettings
                    {
                        PortName = GetConfigValue(settings.ConfigurationJson, "PortName") ?? "COM3",
                        BaudRate = int.Parse(GetConfigValue(settings.ConfigurationJson, "BaudRate") ?? "9600")
                    },
                    TaxId = settings.TaxId,
                    CompanyName = settings.CompanyName,
                    Region = settings.Region
                };

                await provider.InitializeAsync(fiscalSettings);

                return provider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fiscal printer provider {ProviderId} for tenant {TenantId}",
                    providerId, tenantId);
                return null;
            }
        }

        public async Task<IFiscalPrinterProvider?> GetActiveProviderAsync(Guid? tenantId)
        {
            try
            {
                var settings = await GetActiveFiscalPrinterSettingsAsync(tenantId);

                if (settings == null)
                {
                    _logger.LogWarning("No active fiscal printer configured for tenant {TenantId}", tenantId);
                    return null;
                }

                return await GetProviderAsync(settings.ProviderId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active fiscal printer provider for tenant {TenantId}", tenantId);
                return null;
            }
        }

        public async Task<List<IFiscalPrinterProvider>> GetAvailableProvidersAsync(Guid? tenantId)
        {
            try
            {
                var allSettings = await GetAllFiscalPrinterSettingsAsync(tenantId);
                var availableProviders = new List<IFiscalPrinterProvider>();

                foreach (var settings in allSettings.Where(s => s.IsEnabled))
                {
                    var provider = await GetProviderAsync(settings.ProviderId, tenantId);
                    if (provider != null)
                    {
                        availableProviders.Add(provider);
                    }
                }

                return availableProviders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available fiscal printer providers for tenant {TenantId}", tenantId);
                return new List<IFiscalPrinterProvider>();
            }
        }

        public List<IFiscalPrinterProvider> GetAllProviders() => _allProviders.ToList();

        public async Task<TenantFiscalPrinterSettings?> GetFiscalPrinterSettingsAsync(Guid? tenantId)
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
                _logger.LogError(ex, "Error getting fiscal printer settings for tenant {TenantId}", tenantId);
                return null;
            }
        }

        public async Task<TenantFiscalPrinterSettings?> GetActiveFiscalPrinterSettingsAsync(Guid? tenantId)
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
                _logger.LogError(ex, "Error getting active fiscal printer settings for tenant {TenantId}", tenantId);
                return null;
            }
        }

        public async Task<List<TenantFiscalPrinterSettings>> GetAllFiscalPrinterSettingsAsync(Guid? tenantId)
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
                _logger.LogError(ex, "Error getting all fiscal printer settings for tenant {TenantId}", tenantId);
                return new List<TenantFiscalPrinterSettings>();
            }
        }

        private string? GetConfigValue(string configJson, string key)
        {
            try
            {
                var doc = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonDocument>(configJson);
                if (doc != null && doc.RootElement.TryGetProperty(key, out var value))
                {
                    return value.GetString();
                }
            }
            catch
            {
                // Ignore parse errors
            }
            return null;
        }
    }
}
