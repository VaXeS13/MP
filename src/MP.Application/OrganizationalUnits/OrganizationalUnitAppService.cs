using MP.Domain.OrganizationalUnits;
using MP.Domain.Settings;
using MP.OrganizationalUnits.Dtos;
using MP.Permissions;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Volo.Abp.Users;

namespace MP.OrganizationalUnits
{
    [Authorize]
    public class OrganizationalUnitAppService : ApplicationService, IOrganizationalUnitAppService
    {
        private readonly OrganizationalUnitManager _unitManager;
        private readonly IOrganizationalUnitRepository _unitRepository;
        private readonly IOrganizationalUnitSettingsRepository _settingsRepository;
        private readonly IUserOrganizationalUnitRepository _userUnitRepository;
        private readonly ICurrentTenant _currentTenant;
        private readonly ICurrentUser _currentUser;

        public OrganizationalUnitAppService(
            OrganizationalUnitManager unitManager,
            IOrganizationalUnitRepository unitRepository,
            IOrganizationalUnitSettingsRepository settingsRepository,
            IUserOrganizationalUnitRepository userUnitRepository,
            ICurrentTenant currentTenant,
            ICurrentUser currentUser)
        {
            _unitManager = unitManager;
            _unitRepository = unitRepository;
            _settingsRepository = settingsRepository;
            _userUnitRepository = userUnitRepository;
            _currentTenant = currentTenant;
            _currentUser = currentUser;
        }

        [Authorize(MPPermissions.OrganizationalUnits.Create)]
        public async Task<OrganizationalUnitDto> CreateAsync(CreateUpdateOrganizationalUnitDto input)
        {
            var tenantId = _currentTenant.Id;

            var unit = new OrganizationalUnit(
                GuidGenerator.Create(),
                input.Name,
                input.Code,
                tenantId);

            unit.UpdateContactInfo(input.Address, input.City, input.PostalCode, input.Email, input.Phone);

            await _unitRepository.InsertAsync(unit);

            var settings = new OrganizationalUnitSettings(
                GuidGenerator.Create(),
                unit.Id,
                tenantId,
                false);

            await _settingsRepository.InsertAsync(settings);

            return ObjectMapper.Map<OrganizationalUnit, OrganizationalUnitDto>(unit);
        }

        public async Task<OrganizationalUnitDto> GetAsync(Guid id)
        {
            await _unitManager.ValidateUserAccessAsync(_currentUser.GetId(), id, _currentTenant.Id);

            var unit = await _unitRepository.GetAsync(id);
            var settings = await _settingsRepository.GetByOrganizationalUnitAsync(_currentTenant.Id, id);

            var dto = ObjectMapper.Map<OrganizationalUnit, OrganizationalUnitDto>(unit);
            if (settings != null)
            {
                dto.Settings = ObjectMapper.Map<OrganizationalUnitSettings, OrganizationalUnitSettingsDto>(settings);
            }

            return dto;
        }

        public async Task<List<OrganizationalUnitDto>> GetListAsync()
        {
            var userId = _currentUser.GetId();
            var tenantId = _currentTenant.Id;

            var unitIds = await _userUnitRepository.GetUserOrganizationalUnitIdsAsync(tenantId, userId);
            var unitDtos = new List<OrganizationalUnitDto>();

            foreach (var unitId in unitIds)
            {
                var unit = await _unitRepository.GetAsync(unitId);
                var settings = await _settingsRepository.GetByOrganizationalUnitAsync(tenantId, unit.Id);

                var dto = ObjectMapper.Map<OrganizationalUnit, OrganizationalUnitDto>(unit);
                if (settings != null)
                {
                    dto.Settings = ObjectMapper.Map<OrganizationalUnitSettings, OrganizationalUnitSettingsDto>(settings);
                }

                unitDtos.Add(dto);
            }

            return unitDtos;
        }

        [Authorize(MPPermissions.OrganizationalUnits.Edit)]
        public async Task<OrganizationalUnitDto> UpdateAsync(Guid id, CreateUpdateOrganizationalUnitDto input)
        {
            await _unitManager.ValidateUserAccessAsync(_currentUser.GetId(), id, _currentTenant.Id);

            var unit = await _unitRepository.GetAsync(id);

            unit.SetName(input.Name);
            unit.SetCode(input.Code);
            unit.UpdateContactInfo(input.Address, input.City, input.PostalCode, input.Email, input.Phone);

            if (input.IsActive && !unit.IsActive)
            {
                unit.Activate();
            }
            else if (!input.IsActive && unit.IsActive)
            {
                unit.Deactivate();
            }

            await _unitRepository.UpdateAsync(unit);

            return ObjectMapper.Map<OrganizationalUnit, OrganizationalUnitDto>(unit);
        }

        [Authorize(MPPermissions.OrganizationalUnits.Delete)]
        public async Task DeleteAsync(Guid id)
        {
            await _unitManager.ValidateUserAccessAsync(_currentUser.GetId(), id, _currentTenant.Id);
            await _unitRepository.DeleteAsync(id);
        }

        public async Task<CurrentUnitDto> GetCurrentUnitAsync()
        {
            var userId = _currentUser.GetId();
            var tenantId = _currentTenant.Id;

            var unitIds = await _userUnitRepository.GetUserOrganizationalUnitIdsAsync(tenantId, userId);
            if (!unitIds.Any())
            {
                throw new BusinessException("CurrentUnit.NoUnitsAvailable", "User has no accessible organizational units");
            }

            var unitId = unitIds.First();
            var unit = await _unitRepository.GetAsync(unitId);
            var settings = await _settingsRepository.GetByOrganizationalUnitAsync(tenantId, unitId);

            var dto = new CurrentUnitDto
            {
                UnitId = unit.Id,
                UnitName = unit.Name,
                UnitCode = unit.Code,
                Currency = settings?.Currency ?? "PLN",
                UserRole = null
            };

            if (settings != null)
            {
                dto.Settings = ObjectMapper.Map<OrganizationalUnitSettings, OrganizationalUnitSettingsDto>(settings);
            }

            return dto;
        }

        public async Task<SwitchUnitDto> SwitchUnitAsync(Guid unitId)
        {
            var userId = _currentUser.GetId();
            var tenantId = _currentTenant.Id;

            await _unitManager.ValidateUserAccessAsync(userId, unitId, tenantId);

            var unit = await _unitRepository.GetAsync(unitId);

            return new SwitchUnitDto
            {
                UnitId = unit.Id,
                UnitName = unit.Name,
                CookieSet = true
            };
        }

        public async Task<OrganizationalUnitSettingsDto> GetSettingsAsync(Guid unitId)
        {
            await _unitManager.ValidateUserAccessAsync(_currentUser.GetId(), unitId, _currentTenant.Id);

            var settings = await _settingsRepository.GetByOrganizationalUnitAsync(_currentTenant.Id, unitId);

            if (settings == null)
            {
                throw new BusinessException("UnitSettings.NotFound", $"Settings not found for unit {unitId}");
            }

            return ObjectMapper.Map<OrganizationalUnitSettings, OrganizationalUnitSettingsDto>(settings);
        }

        [Authorize(MPPermissions.OrganizationalUnits.Edit)]
        public async Task<OrganizationalUnitSettingsDto> UpdateSettingsAsync(Guid unitId, UpdateUnitSettingsDto input)
        {
            await _unitManager.ValidateUserAccessAsync(_currentUser.GetId(), unitId, _currentTenant.Id);

            var settings = await _settingsRepository.GetByOrganizationalUnitAsync(_currentTenant.Id, unitId);

            if (settings == null)
            {
                throw new BusinessException("UnitSettings.NotFound", $"Settings not found for unit {unitId}");
            }

            settings.UpdateCurrency(input.Currency);

            if (input.EnabledPaymentProviders != null && input.EnabledPaymentProviders.Any())
            {
                settings.UpdatePaymentProviders(input.EnabledPaymentProviders);
            }

            if (!string.IsNullOrEmpty(input.DefaultPaymentProvider))
            {
                settings.SetDefaultPaymentProvider(input.DefaultPaymentProvider);
            }

            settings.UpdateBranding(input.LogoUrl, input.BannerText);

            await _settingsRepository.UpdateAsync(settings);

            return ObjectMapper.Map<OrganizationalUnitSettings, OrganizationalUnitSettingsDto>(settings);
        }
    }
}
