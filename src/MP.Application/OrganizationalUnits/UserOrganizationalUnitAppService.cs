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
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;
using Volo.Abp.Uow;

namespace MP.OrganizationalUnits
{
    [Authorize]
    public class UserOrganizationalUnitAppService : ApplicationService, IUserOrganizationalUnitAppService
    {
        private readonly UserOrganizationalUnitManager _userUnitManager;
        private readonly IUserOrganizationalUnitRepository _userUnitRepository;
        private readonly IOrganizationalUnitRepository _unitRepository;
        private readonly IRepository<IdentityUser, Guid> _userRepository;
        private readonly IRepository<IdentityRole, Guid> _roleRepository;
        private readonly ICurrentTenant _currentTenant;
        private readonly ISettingProvider _settingProvider;

        public UserOrganizationalUnitAppService(
            UserOrganizationalUnitManager userUnitManager,
            IUserOrganizationalUnitRepository userUnitRepository,
            IOrganizationalUnitRepository unitRepository,
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<IdentityRole, Guid> roleRepository,
            ICurrentTenant currentTenant,
            ISettingProvider settingProvider)
        {
            _userUnitManager = userUnitManager;
            _userUnitRepository = userUnitRepository;
            _unitRepository = unitRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _currentTenant = currentTenant;
            _settingProvider = settingProvider;
        }

        /// <summary>
        /// Gets current user's organizational units
        /// </summary>
        public async Task<List<MyUnitDto>> GetMyUnitsAsync()
        {
            var userId = CurrentUser.Id.Value;
            var tenantId = _currentTenant.Id;

            // Get user's memberships
            var memberships = await _userUnitRepository.GetUserMembershipsAsync(
                tenantId,
                userId,
                maxResultCount: int.MaxValue);

            if (!memberships.Any())
                return new List<MyUnitDto>();

            // Get unit details
            var unitIds = memberships.Select(m => m.OrganizationalUnitId).ToList();
            var allUnits = await _unitRepository.GetListAsync(tenantId: tenantId);
            var units = allUnits.Where(u => unitIds.Contains(u.Id)).ToList();

            // Get role names
            var roleIds = memberships.Where(m => m.RoleId.HasValue).Select(m => m.RoleId.Value).Distinct().ToList();
            var roles = roleIds.Any()
                ? await _roleRepository.GetListAsync(r => roleIds.Contains(r.Id))
                : new List<IdentityRole>();

            var roleNameMap = roles.ToDictionary(r => r.Id, r => r.Name);

            // Get tenant currency
            var currencySettingValue = await _settingProvider.GetOrNullAsync(MPSettings.Tenant.Currency);
            var currency = currencySettingValue ?? "PLN";

            // Map to DTOs
            var dtos = new List<MyUnitDto>();
            foreach (var membership in memberships)
            {
                var unit = units.FirstOrDefault(u => u.Id == membership.OrganizationalUnitId);
                if (unit == null)
                    continue;

                var roleName = membership.RoleId.HasValue && roleNameMap.TryGetValue(membership.RoleId.Value, out var role)
                    ? role
                    : null;

                dtos.Add(new MyUnitDto
                {
                    UnitId = unit.Id,
                    UnitName = unit.Name,
                    UnitCode = unit.Code,
                    Role = roleName,
                    Currency = currency
                });
            }

            return dtos;
        }

        /// <summary>
        /// Switches current user's active organizational unit
        /// </summary>
        [UnitOfWork]
        public async Task<SwitchUnitDto> SwitchUnitAsync(Guid unitId)
        {
            var userId = CurrentUser.Id.Value;
            var tenantId = _currentTenant.Id;

            // Verify user has access to this unit
            var isMember = await _userUnitRepository.IsMemberAsync(tenantId, userId, unitId);
            if (!isMember)
                throw new BusinessException("UserOrganizationalUnit.NotMember", "User does not have access to this organizational unit");

            // Get unit details
            var unit = await _unitRepository.GetAsync(unitId);
            if (unit == null)
                throw new BusinessException("OrganizationalUnit.NotFound", "Organizational unit not found");

            // Note: Cookie handling would be done in the controller/client
            // The service just validates the switch is valid

            return new SwitchUnitDto
            {
                UnitId = unit.Id,
                UnitName = unit.Name,
                CookieSet = true
            };
        }

        /// <summary>
        /// Gets all users assigned to a specific organizational unit
        /// </summary>
        [Authorize(MPPermissions.OrganizationalUnits.ManageUsers)]
        public async Task<List<UserInUnitDto>> GetUsersInUnitAsync(Guid unitId)
        {
            var tenantId = _currentTenant.Id;

            // Verify unit exists
            var unit = await _unitRepository.GetAsync(unitId);
            if (unit == null)
                throw new BusinessException("OrganizationalUnit.NotFound", "Organizational unit not found");

            // Get user IDs in unit
            var userIds = await _userUnitRepository.GetOrganizationalUnitUserIdsAsync(tenantId, unitId);

            if (!userIds.Any())
                return new List<UserInUnitDto>();

            // Get user details
            var users = await _userRepository.GetListAsync(u => userIds.Contains(u.Id));

            // Get memberships for role info
            var memberships = new Dictionary<Guid, UserOrganizationalUnit>();
            foreach (var userId in userIds)
            {
                var userMemberships = await _userUnitRepository.GetUserMembershipsAsync(
                    tenantId,
                    userId,
                    maxResultCount: int.MaxValue);

                var membership = userMemberships.FirstOrDefault(m => m.OrganizationalUnitId == unitId);
                if (membership != null)
                    memberships[userId] = membership;
            }

            // Get role names
            var roleIds = memberships.Values
                .Where(m => m.RoleId.HasValue)
                .Select(m => m.RoleId.Value)
                .Distinct()
                .ToList();

            var roles = roleIds.Any()
                ? await _roleRepository.GetListAsync(r => roleIds.Contains(r.Id))
                : new List<IdentityRole>();

            var roleNameMap = roles.ToDictionary(r => r.Id, r => r.Name);

            // Map to DTOs
            var dtos = new List<UserInUnitDto>();
            foreach (var user in users)
            {
                if (!memberships.TryGetValue(user.Id, out var membership))
                    continue;

                var roleName = membership.RoleId.HasValue && roleNameMap.TryGetValue(membership.RoleId.Value, out var role)
                    ? role
                    : null;

                dtos.Add(new UserInUnitDto
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Role = roleName,
                    AssignedAt = membership.AssignedAt,
                    IsActive = membership.IsActive
                });
            }

            return dtos.OrderBy(u => u.UserName).ToList();
        }

        /// <summary>
        /// Assigns a user to an organizational unit
        /// </summary>
        [Authorize(MPPermissions.OrganizationalUnits.ManageUsers)]
        [UnitOfWork]
        public async Task<UserInUnitDto> AssignUserToUnitAsync(Guid unitId, AssignUserDto input)
        {
            // Validate input
            if (input.UserId == Guid.Empty)
                throw new BusinessException("User.InvalidId", "User ID is required");

            var tenantId = _currentTenant.Id;

            // Verify unit exists
            var unit = await _unitRepository.GetAsync(unitId);
            if (unit == null)
                throw new BusinessException("OrganizationalUnit.NotFound", "Organizational unit not found");

            // Verify user exists
            var user = await _userRepository.FindAsync(input.UserId);
            if (user == null)
                throw new BusinessException("User.NotFound", "User not found");

            // Assign user to unit using domain service
            var assignment = await _userUnitManager.AssignUserToUnitAsync(
                input.UserId,
                unitId,
                input.RoleId);

            // Get role name if assigned
            var roleName = assignment.RoleId.HasValue
                ? (await _roleRepository.FindAsync(assignment.RoleId.Value))?.Name
                : null;

            return new UserInUnitDto
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = roleName,
                AssignedAt = assignment.AssignedAt,
                IsActive = assignment.IsActive
            };
        }

        /// <summary>
        /// Removes a user from an organizational unit
        /// </summary>
        [Authorize(MPPermissions.OrganizationalUnits.ManageUsers)]
        [UnitOfWork]
        public async Task RemoveUserFromUnitAsync(Guid unitId, Guid userId)
        {
            var tenantId = _currentTenant.Id;

            // Verify unit exists
            var unit = await _unitRepository.GetAsync(unitId);
            if (unit == null)
                throw new BusinessException("OrganizationalUnit.NotFound", "Organizational unit not found");

            // Verify user exists
            var user = await _userRepository.FindAsync(userId);
            if (user == null)
                throw new BusinessException("User.NotFound", "User not found");

            // Remove user from unit using domain service
            await _userUnitManager.RemoveUserFromUnitAsync(userId, unitId);
        }

        /// <summary>
        /// Updates a user's role within an organizational unit
        /// </summary>
        [Authorize(MPPermissions.OrganizationalUnits.ManageUsers)]
        [UnitOfWork]
        public async Task<UserInUnitDto> UpdateUserRoleAsync(Guid unitId, Guid userId, UpdateUserRoleDto input)
        {
            var tenantId = _currentTenant.Id;

            // Verify unit exists
            var unit = await _unitRepository.GetAsync(unitId);
            if (unit == null)
                throw new BusinessException("OrganizationalUnit.NotFound", "Organizational unit not found");

            // Verify user exists
            var user = await _userRepository.FindAsync(userId);
            if (user == null)
                throw new BusinessException("User.NotFound", "User not found");

            // Update role using domain service
            var assignment = await _userUnitManager.UpdateUserRoleInUnitAsync(userId, unitId, input.RoleId);

            // Get role name if assigned
            var roleName = assignment.RoleId.HasValue
                ? (await _roleRepository.FindAsync(assignment.RoleId.Value))?.Name
                : null;

            return new UserInUnitDto
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = roleName,
                AssignedAt = assignment.AssignedAt,
                IsActive = assignment.IsActive
            };
        }

        /// <summary>
        /// Joins authenticated user to an organizational unit using a registration code
        /// </summary>
        [AllowAnonymous]
        [UnitOfWork]
        public async Task<JoinUnitResultDto> JoinUnitWithCodeAsync(string code)
        {
            var userId = CurrentUser.Id.Value;
            var tenantId = _currentTenant.Id;

            // Validate code provided
            if (string.IsNullOrWhiteSpace(code))
                throw new BusinessException("RegistrationCode.Required", "Registration code is required");

            // Join using domain service
            var assignment = await _userUnitManager.JoinUnitWithCodeAsync(userId, code);

            // Get unit details
            var unit = await _unitRepository.GetAsync(assignment.OrganizationalUnitId);
            if (unit == null)
                throw new BusinessException("OrganizationalUnit.NotFound", "Organizational unit not found");

            return new JoinUnitResultDto
            {
                UnitId = unit.Id,
                UnitName = unit.Name
            };
        }
    }
}
