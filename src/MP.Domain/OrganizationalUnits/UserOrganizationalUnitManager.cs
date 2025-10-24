using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.OrganizationalUnits
{
    /// <summary>
    /// Domain service for managing user assignments to organizational units
    /// Encapsulates business logic for user membership, role assignment, and access control
    /// </summary>
    public class UserOrganizationalUnitManager : DomainService
    {
        private readonly IUserOrganizationalUnitRepository _userUnitRepository;
        private readonly IOrganizationalUnitRepository _unitRepository;
        private readonly RegistrationCodeManager _codeManager;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ICurrentTenant _currentTenant;
        private readonly IRepository<IdentityRole, Guid> _roleRepository;

        public UserOrganizationalUnitManager(
            IUserOrganizationalUnitRepository userUnitRepository,
            IOrganizationalUnitRepository unitRepository,
            RegistrationCodeManager codeManager,
            IGuidGenerator guidGenerator,
            ICurrentTenant currentTenant,
            IRepository<IdentityRole, Guid> roleRepository)
        {
            _userUnitRepository = userUnitRepository;
            _unitRepository = unitRepository;
            _codeManager = codeManager;
            _guidGenerator = guidGenerator;
            _currentTenant = currentTenant;
            _roleRepository = roleRepository;
        }

        /// <summary>
        /// Assigns a user to an organizational unit with optional role
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="unitId">The organizational unit identifier</param>
        /// <param name="roleId">Optional role identifier</param>
        /// <returns>The created user-unit assignment</returns>
        /// <exception cref="BusinessException">Thrown if user already assigned to unit</exception>
        /// <exception cref="BusinessException">Thrown if unit doesn't exist</exception>
        public virtual async Task<UserOrganizationalUnit> AssignUserToUnitAsync(
            Guid userId,
            Guid unitId,
            Guid? roleId = null)
        {
            var tenantId = _currentTenant.Id;

            // Validate unit exists
            var unit = await _unitRepository.GetAsync(unitId);
            if (unit == null)
                throw new BusinessException(
                    "UserOrganizationalUnit.UnitNotFound",
                    "Organizational unit not found");

            // Check if user is already assigned to this unit
            var isMember = await _userUnitRepository.IsMemberAsync(tenantId, userId, unitId);
            if (isMember)
                throw new BusinessException(
                    "UserOrganizationalUnit.AlreadyAssigned",
                    "User is already assigned to this organizational unit");

            // Validate role if provided
            if (roleId.HasValue)
            {
                var role = await _roleRepository.FindAsync(roleId.Value);
                if (role == null)
                    throw new BusinessException(
                        "UserOrganizationalUnit.RoleNotFound",
                        "Specified role does not exist");
            }

            // Create the assignment
            var assignment = new UserOrganizationalUnit(
                id: _guidGenerator.Create(),
                userId: userId,
                organizationalUnitId: unitId,
                roleId: roleId,
                tenantId: tenantId);

            await _userUnitRepository.InsertAsync(assignment);

            return assignment;
        }

        /// <summary>
        /// Joins a user to an organizational unit using a registration code
        /// Validates code, increments usage count, assigns user with code's role
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="registrationCode">The registration code string</param>
        /// <returns>The created user-unit assignment</returns>
        /// <exception cref="BusinessException">Thrown if code is invalid, expired, or usage limit reached</exception>
        /// <exception cref="BusinessException">Thrown if user already assigned to unit</exception>
        public virtual async Task<UserOrganizationalUnit> JoinUnitWithCodeAsync(
            Guid userId,
            string registrationCode)
        {
            var tenantId = _currentTenant.Id;

            if (string.IsNullOrWhiteSpace(registrationCode))
                throw new BusinessException(
                    "UserOrganizationalUnit.CodeRequired",
                    "Registration code is required");

            // Validate and retrieve the code
            var code = await _codeManager.ValidateCodeAsync(tenantId, registrationCode);

            // Assign user to unit using code's role
            var assignment = await AssignUserToUnitAsync(
                userId,
                code.OrganizationalUnitId,
                code.RoleId);

            // Increment code usage count
            await _codeManager.UseCodeAsync(code.Id);

            return assignment;
        }

        /// <summary>
        /// Removes a user from an organizational unit
        /// Prevents removal of last admin to prevent lock-out
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="unitId">The organizational unit identifier</param>
        /// <exception cref="BusinessException">Thrown if user not in unit</exception>
        /// <exception cref="BusinessException">Thrown if user is last admin in unit</exception>
        public virtual async Task RemoveUserFromUnitAsync(
            Guid userId,
            Guid unitId)
        {
            var tenantId = _currentTenant.Id;

            // Check if user is member
            var isMember = await _userUnitRepository.IsMemberAsync(tenantId, userId, unitId);
            if (!isMember)
                throw new BusinessException(
                    "UserOrganizationalUnit.NotMember",
                    "User is not assigned to this organizational unit");

            // Check if this is the last admin (prevent lock-out)
            if (await IsLastAdminAsync(tenantId, unitId, userId))
                throw new BusinessException(
                    "UserOrganizationalUnit.CannotRemoveLastAdmin",
                    "Cannot remove the last administrator from the organizational unit");

            // Remove the user from unit
            await _userUnitRepository.RemoveUserFromUnitAsync(tenantId, userId, unitId);
        }

        /// <summary>
        /// Updates a user's role within an organizational unit
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="unitId">The organizational unit identifier</param>
        /// <param name="newRoleId">New role identifier (can be null to remove role)</param>
        /// <returns>The updated user-unit assignment</returns>
        /// <exception cref="BusinessException">Thrown if user not in unit</exception>
        /// <exception cref="BusinessException">Thrown if role doesn't exist</exception>
        public virtual async Task<UserOrganizationalUnit> UpdateUserRoleInUnitAsync(
            Guid userId,
            Guid unitId,
            Guid? newRoleId)
        {
            var tenantId = _currentTenant.Id;

            // Validate role if provided
            if (newRoleId.HasValue)
            {
                var role = await _roleRepository.FindAsync(newRoleId.Value);
                if (role == null)
                    throw new BusinessException(
                        "UserOrganizationalUnit.RoleNotFound",
                        "Specified role does not exist");
            }

            // Get the membership
            var memberships = await _userUnitRepository.GetUserMembershipsAsync(
                tenantId,
                userId,
                maxResultCount: int.MaxValue);

            var membership = memberships.FirstOrDefault(m => m.OrganizationalUnitId == unitId);
            if (membership == null)
                throw new BusinessException(
                    "UserOrganizationalUnit.NotMember",
                    "User is not assigned to this organizational unit");

            // Update the role
            membership.UpdateRole(newRoleId);

            await _userUnitRepository.UpdateAsync(membership);

            return membership;
        }

        /// <summary>
        /// Soft-deactivates a user in an organizational unit
        /// User loses access but history is preserved
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="unitId">The organizational unit identifier</param>
        /// <exception cref="BusinessException">Thrown if user not in unit</exception>
        /// <exception cref="BusinessException">Thrown if user is last admin in unit</exception>
        public virtual async Task DeactivateUserInUnitAsync(
            Guid userId,
            Guid unitId)
        {
            var tenantId = _currentTenant.Id;

            // Check if user is member
            var isMember = await _userUnitRepository.IsMemberAsync(tenantId, userId, unitId);
            if (!isMember)
                throw new BusinessException(
                    "UserOrganizationalUnit.NotMember",
                    "User is not assigned to this organizational unit");

            // Check if this is the last admin (prevent lock-out)
            if (await IsLastAdminAsync(tenantId, unitId, userId))
                throw new BusinessException(
                    "UserOrganizationalUnit.CannotDeactivateLastAdmin",
                    "Cannot deactivate the last administrator in the organizational unit");

            // Get the membership
            var memberships = await _userUnitRepository.GetUserMembershipsAsync(
                tenantId,
                userId,
                maxResultCount: int.MaxValue);

            var membership = memberships.FirstOrDefault(m => m.OrganizationalUnitId == unitId);
            if (membership == null)
                throw new BusinessException(
                    "UserOrganizationalUnit.NotMember",
                    "User is not assigned to this organizational unit");

            // Deactivate the membership
            membership.Deactivate();

            await _userUnitRepository.UpdateAsync(membership);
        }

        /// <summary>
        /// Retrieves all organizational units a user has access to
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="includeInactive">Include inactive memberships if true</param>
        /// <returns>List of organizational units user has access to</returns>
        public virtual async Task<List<OrganizationalUnit>> GetUserUnitsAsync(
            Guid userId,
            Guid tenantId,
            bool includeInactive = false)
        {
            // Get user's organizational unit IDs
            var unitIds = await _userUnitRepository.GetUserOrganizationalUnitIdsAsync(tenantId, userId);

            if (unitIds.Count == 0)
                return new List<OrganizationalUnit>();

            // Get the units
            var units = await _unitRepository.GetListAsync(
                tenantId: tenantId,
                isActive: includeInactive ? null : true);

            // Filter to only units user belongs to
            return units
                .Where(u => unitIds.Contains(u.Id))
                .ToList();
        }

        /// <summary>
        /// Checks if a user is the last active admin in a unit
        /// Used to prevent lock-out when removing or deactivating last admin
        /// An admin is defined as a user with a role assigned in the unit
        /// </summary>
        private async Task<bool> IsLastAdminAsync(
            Guid? tenantId,
            Guid unitId,
            Guid userId)
        {
            // Get all members in unit
            var unitMemberIds = await _userUnitRepository.GetOrganizationalUnitUserIdsAsync(tenantId, unitId);

            if (unitMemberIds.Count <= 1)
                return true; // Only one member total (the current user)

            // Get all memberships for this unit to check roles
            var unitMemberships = new List<UserOrganizationalUnit>();
            foreach (var memberId in unitMemberIds)
            {
                var memberships = await _userUnitRepository.GetUserMembershipsAsync(
                    tenantId,
                    memberId,
                    maxResultCount: int.MaxValue);

                var unitMembership = memberships.FirstOrDefault(m => m.OrganizationalUnitId == unitId);
                if (unitMembership != null)
                    unitMemberships.Add(unitMembership);
            }

            // Check if current user is an admin (has a role)
            var userMembership = unitMemberships.FirstOrDefault(m => m.UserId == userId);
            if (userMembership?.RoleId == null)
                return false; // User is not an admin, can be removed freely

            // Count other admins (excluding current user)
            var otherAdmins = unitMemberships.Count(m => m.UserId != userId && m.RoleId != null);

            return otherAdmins == 0; // Last admin if no other admins exist
        }
    }
}
