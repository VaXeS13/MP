using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MP.Domain.Settings;
using MP.Permissions;
using Volo.Abp.Application.Services;
using Volo.Abp.SettingManagement;

namespace MP.Booths
{
    [Authorize(MPPermissions.Booths.ManageSettings)]
    public class BoothSettingsAppService : ApplicationService, IBoothSettingsAppService
    {
        private readonly ISettingManager _settingManager;

        public BoothSettingsAppService(ISettingManager settingManager)
        {
            _settingManager = settingManager;
        }

        public async Task<BoothSettingsDto> GetAsync()
        {
            var minimumGapDays = await _settingManager.GetOrNullForCurrentTenantAsync(MPSettings.Booths.MinimumGapDays);
            var minimumRentalDays = await _settingManager.GetOrNullForCurrentTenantAsync(MPSettings.Booths.MinimumRentalDays);

            return new BoothSettingsDto
            {
                MinimumGapDays = int.TryParse(minimumGapDays, out var gap) ? gap : 7,
                MinimumRentalDays = int.TryParse(minimumRentalDays, out var rental) ? rental : 7
            };
        }

        public async Task UpdateAsync(BoothSettingsDto input)
        {
            await _settingManager.SetForCurrentTenantAsync(
                MPSettings.Booths.MinimumGapDays,
                input.MinimumGapDays.ToString()
            );

            await _settingManager.SetForCurrentTenantAsync(
                MPSettings.Booths.MinimumRentalDays,
                input.MinimumRentalDays.ToString()
            );
        }
    }
}
