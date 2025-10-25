using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MP.Domain.Booths;
using MP.Domain.Settings;
using MP.Domain.OrganizationalUnits;
using MP.Permissions;
using Volo.Abp.Application.Services;
using Volo.Abp.SettingManagement;

namespace MP.Tenants
{
    [Authorize(MPPermissions.Tenant.ManageCurrency)]
    public class TenantCurrencyAppService : ApplicationService, ITenantCurrencyAppService
    {
        private readonly ISettingManager _settingManager;
        private readonly ICurrentOrganizationalUnit _currentOrganizationalUnit;

        public TenantCurrencyAppService(
            ISettingManager settingManager,
            ICurrentOrganizationalUnit currentOrganizationalUnit)
        {
            _settingManager = settingManager;
            _currentOrganizationalUnit = currentOrganizationalUnit;
        }

        public async Task<TenantCurrencyDto> GetTenantCurrencyAsync()
        {
            // Get organizational unit context if available for future unit-level overrides
            var organizationalUnitId = _currentOrganizationalUnit.Id;

            var currencySetting = await _settingManager.GetOrNullForCurrentTenantAsync(MPSettings.Tenant.Currency);

            Currency currency = Currency.PLN; // Default
            if (!string.IsNullOrEmpty(currencySetting))
            {
                if (Enum.TryParse<Currency>(currencySetting, out var parsedCurrency))
                {
                    currency = parsedCurrency;
                }
                // Fallback: try parsing as int for backward compatibility
                else if (int.TryParse(currencySetting, out var currencyValue))
                {
                    currency = (Currency)currencyValue;
                }
            }

            return new TenantCurrencyDto
            {
                Currency = currency
            };
        }

        public async Task UpdateTenantCurrencyAsync(TenantCurrencyDto input)
        {
            // Store as string name (e.g., "PLN", "EUR", "USD")
            await _settingManager.SetForCurrentTenantAsync(
                MPSettings.Tenant.Currency,
                input.Currency.ToString()
            );
        }
    }
}
