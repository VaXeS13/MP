using MP.Domain.OrganizationalUnits;
using MP.OrganizationalUnits.Dtos;
using MP.Permissions;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace MP.OrganizationalUnits
{
    [Authorize]
    public class RegistrationCodeAppService : ApplicationService, IRegistrationCodeAppService
    {
        private readonly RegistrationCodeManager _codeManager;
        private readonly OrganizationalUnitManager _unitManager;
        private readonly IOrganizationalUnitRegistrationCodeRepository _codeRepository;
        private readonly IOrganizationalUnitRepository _unitRepository;
        private readonly ICurrentTenant _currentTenant;

        public RegistrationCodeAppService(
            RegistrationCodeManager codeManager,
            OrganizationalUnitManager unitManager,
            IOrganizationalUnitRegistrationCodeRepository codeRepository,
            IOrganizationalUnitRepository unitRepository,
            ICurrentTenant currentTenant)
        {
            _codeManager = codeManager;
            _unitManager = unitManager;
            _codeRepository = codeRepository;
            _unitRepository = unitRepository;
            _currentTenant = currentTenant;
        }

        [Authorize(MPPermissions.Tenant.ManageOrganizationalUnits)]
        [UnitOfWork]
        public async Task<RegistrationCodeDto> GenerateCodeAsync(
            Guid organizationalUnitId,
            CreateRegistrationCodeDto input)
        {
            // Validate unit exists and user has access
            var unit = await _unitRepository.GetAsync(organizationalUnitId);
            if (unit == null)
                throw new BusinessException("OrganizationalUnit.NotFound", "Organizational unit not found");

            // Generate code via domain service
            var registrationCode = await _codeManager.GenerateCodeAsync(
                organizationalUnitId,
                input.RoleId,
                input.MaxUsageCount,
                input.ExpirationDays);

            // Save to repository
            await _codeRepository.InsertAsync(registrationCode);

            // Map to DTO
            return ObjectMapper.Map<OrganizationalUnitRegistrationCode, RegistrationCodeDto>(registrationCode);
        }

        [AllowAnonymous]
        [UnitOfWork]
        public async Task<JoinUnitResultDto> JoinUnitWithCodeAsync(string code)
        {
            // Validate and get tenant from current context
            // Note: _currentTenant.Id can be null for host tenant
            var tenantId = _currentTenant.Id;

            // Validate code
            var registrationCode = await _codeManager.ValidateCodeAsync(tenantId ?? Guid.Empty, code);

            // Get unit details
            var unit = await _unitRepository.GetAsync(registrationCode.OrganizationalUnitId);
            if (unit == null)
                throw new BusinessException("OrganizationalUnit.NotFound", "Organizational unit not found");

            // If user is not authenticated, return result indicating registration is needed
            if (CurrentUser == null || CurrentUser.Id == null)
            {
                return new JoinUnitResultDto
                {
                    UnitId = unit.Id,
                    UnitName = unit.Name,
                    RequiresRegistration = true
                };
            }

            // User is authenticated - assign them to the unit
            var userId = CurrentUser.Id.Value;

            // Mark code as used
            var updatedCode = await _codeManager.UseCodeAsync(registrationCode.Id);
            await _codeRepository.UpdateAsync(updatedCode);

            // Assign user to unit with optional role
            var roleId = registrationCode.RoleId;
            await _unitManager.AssignUserToUnitAsync(userId, unit.Id, tenantId, roleId);

            return new JoinUnitResultDto
            {
                UnitId = unit.Id,
                UnitName = unit.Name,
                RequiresRegistration = false
            };
        }

        [AllowAnonymous]
        public async Task<ValidateCodeResultDto> ValidateCodeAsync(string code)
        {
            try
            {
                // Validate and get tenant from current context
                // Note: _currentTenant.Id can be null for host tenant
                var tenantId = _currentTenant.Id;

                // Try to validate code
                var registrationCode = await _codeManager.ValidateCodeAsync(tenantId ?? Guid.Empty, code);

                // Get unit details
                var unit = await _unitRepository.GetAsync(registrationCode.OrganizationalUnitId);
                if (unit == null)
                {
                    return new ValidateCodeResultDto
                    {
                        IsValid = false,
                        Reason = "Organizational unit not found"
                    };
                }

                return new ValidateCodeResultDto
                {
                    IsValid = true,
                    UnitId = unit.Id,
                    UnitName = unit.Name
                };
            }
            catch (BusinessException ex)
            {
                // Map domain exceptions to validation result
                var reason = ex.Code switch
                {
                    "RegistrationCode.NotFound" => "Code not found",
                    "RegistrationCode.Inactive" => "Code is inactive",
                    "RegistrationCode.Expired" => "Code has expired",
                    "RegistrationCode.UsageLimitReached" => "Usage limit reached",
                    _ => "Invalid code"
                };

                return new ValidateCodeResultDto
                {
                    IsValid = false,
                    Reason = reason
                };
            }
        }

        [Authorize(MPPermissions.Tenant.ManageOrganizationalUnits)]
        [UnitOfWork]
        public async Task DeactivateCodeAsync(Guid codeId)
        {
            // Get code
            var code = await _codeRepository.GetAsync(codeId);
            if (code == null)
                throw new BusinessException("RegistrationCode.NotFound", "Registration code not found");

            // Deactivate
            code.Deactivate();
            await _codeRepository.UpdateAsync(code);
        }

        [Authorize(MPPermissions.Tenant.ManageOrganizationalUnits)]
        public async Task<List<RegistrationCodeDto>> ListCodesAsync(Guid organizationalUnitId)
        {
            // Validate unit exists
            var unit = await _unitRepository.GetAsync(organizationalUnitId);
            if (unit == null)
                throw new BusinessException("OrganizationalUnit.NotFound", "Organizational unit not found");

            // Get codes - all codes (active and inactive) for the unit
            var codes = await _codeRepository.GetListAsync(
                tenantId: _currentTenant.Id,
                organizationalUnitId: organizationalUnitId,
                isActive: null  // Get all, not just active
            );

            // Map to DTOs
            var dtos = codes.Select(c => new RegistrationCodeDto
            {
                Id = c.Id,
                OrganizationalUnitId = c.OrganizationalUnitId,
                Code = c.Code,
                RoleId = c.RoleId,
                ExpiresAt = c.ExpiresAt,
                MaxUsageCount = c.MaxUsageCount,
                UsageCount = c.UsageCount,
                LastUsedAt = c.LastUsedAt,
                IsActive = c.IsActive,
                IsExpired = c.IsExpired(),
                IsUsageLimitReached = c.IsUsageLimitReached()
            }).ToList();

            return dtos;
        }
    }
}
