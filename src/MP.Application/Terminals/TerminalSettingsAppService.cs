using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.Domain.Repositories;
using MP.Application.Contracts.Terminals;
using MP.Domain.Terminals;

namespace MP.Application.Terminals
{
    public class TerminalSettingsAppService : ApplicationService, ITerminalSettingsAppService
    {
        private readonly IRepository<TenantTerminalSettings, Guid> _repository;
        private readonly ITerminalPaymentProviderFactory _providerFactory;
        private readonly ILogger<TerminalSettingsAppService> _logger;
        private readonly IDistributedCache<TerminalSettingsDto> _cache;

        public TerminalSettingsAppService(
            IRepository<TenantTerminalSettings, Guid> repository,
            ITerminalPaymentProviderFactory providerFactory,
            ILogger<TerminalSettingsAppService> logger,
            IDistributedCache<TerminalSettingsDto> cache)
        {
            _repository = repository;
            _providerFactory = providerFactory;
            _logger = logger;
            _cache = cache;
        }

        public async Task<TerminalSettingsDto?> GetCurrentTenantSettingsAsync()
        {
            var cacheKey = $"TerminalSettings_Tenant_{CurrentTenant?.Id}";

            var cachedData = await _cache.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    var settings = await _providerFactory.GetTerminalSettingsAsync(CurrentTenant.Id);

                    if (settings == null)
                    {
                        return null;
                    }

                    return ObjectMapper.Map<TenantTerminalSettings, TerminalSettingsDto>(settings);
                },
                () => new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                }
            );

            return cachedData;
        }

        public async Task<TerminalSettingsDto> CreateAsync(CreateTerminalSettingsDto input)
        {
            // Check if settings already exist for this tenant
            var existing = await _providerFactory.GetTerminalSettingsAsync(CurrentTenant.Id);
            if (existing != null)
            {
                throw new Volo.Abp.BusinessException("Terminal settings already exist for this tenant. Use update instead.");
            }

            var settings = new TenantTerminalSettings(
                GuidGenerator.Create(),
                CurrentTenant.Id,
                input.ProviderId,
                input.ProviderId, // displayName - will use provider ID as display name
                input.ConfigurationJson,
                input.Currency,
                input.IsEnabled,
                false, // isActive - set separately if needed
                input.IsSandbox
            );

            if (!string.IsNullOrWhiteSpace(input.Region))
            {
                settings.SetRegion(input.Region);
            }

            await _repository.InsertAsync(settings);

            _logger.LogInformation(
                "Created terminal settings for tenant {TenantId} with provider {ProviderId}",
                CurrentTenant.Id, input.ProviderId);

            await InvalidateCacheAsync();

            return ObjectMapper.Map<TenantTerminalSettings, TerminalSettingsDto>(settings);
        }

        public async Task<TerminalSettingsDto> UpdateAsync(Guid id, UpdateTerminalSettingsDto input)
        {
            var settings = await _repository.GetAsync(id);

            // Verify tenant ownership
            if (settings.TenantId != CurrentTenant.Id)
            {
                throw new Volo.Abp.Authorization.AbpAuthorizationException("Access denied");
            }

            settings.SetProviderId(input.ProviderId);
            settings.SetConfiguration(input.ConfigurationJson);
            settings.SetCurrency(input.Currency);
            settings.SetRegion(input.Region);
            settings.SetSandboxMode(input.IsSandbox);

            if (input.IsEnabled)
            {
                settings.Enable();
            }
            else
            {
                settings.Disable();
            }

            await _repository.UpdateAsync(settings);

            _logger.LogInformation(
                "Updated terminal settings {SettingsId} for tenant {TenantId}",
                id, CurrentTenant.Id);

            await InvalidateCacheAsync();

            return ObjectMapper.Map<TenantTerminalSettings, TerminalSettingsDto>(settings);
        }

        public async Task DeleteAsync(Guid id)
        {
            var settings = await _repository.GetAsync(id);

            // Verify tenant ownership
            if (settings.TenantId != CurrentTenant.Id)
            {
                throw new Volo.Abp.Authorization.AbpAuthorizationException("Access denied");
            }

            await _repository.DeleteAsync(id);

            _logger.LogInformation(
                "Deleted terminal settings {SettingsId} for tenant {TenantId}",
                id, CurrentTenant.Id);

            await InvalidateCacheAsync();
        }

        public Task<List<TerminalProviderInfoDto>> GetAvailableProvidersAsync()
        {
            var providers = _providerFactory.GetAllProviders();

            var result = providers.Select(p => new TerminalProviderInfoDto
            {
                ProviderId = p.ProviderId,
                DisplayName = p.DisplayName,
                Description = p.Description
            }).ToList();

            return Task.FromResult(result);
        }

        private async Task InvalidateCacheAsync()
        {
            var cacheKey = $"TerminalSettings_Tenant_{CurrentTenant?.Id}";
            await _cache.RemoveAsync(cacheKey);
        }
    }
}