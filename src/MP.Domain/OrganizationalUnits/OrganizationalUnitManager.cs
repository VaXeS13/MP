using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace MP.Domain.OrganizationalUnits
{
    /// <summary>
    /// Domain service for managing organizational units
    /// Encapsulates business logic for unit creation, user assignment, and settings management
    /// </summary>
    public class OrganizationalUnitManager : DomainService
    {
        private readonly IOrganizationalUnitRepository _unitRepository;
        private readonly IUserOrganizationalUnitRepository _userUnitRepository;
        private readonly IOrganizationalUnitSettingsRepository _settingsRepository;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ICurrentTenant _currentTenant;

        public OrganizationalUnitManager(
            IOrganizationalUnitRepository unitRepository,
            IUserOrganizationalUnitRepository userUnitRepository,
            IOrganizationalUnitSettingsRepository settingsRepository,
            IGuidGenerator guidGenerator,
            ICurrentTenant currentTenant)
        {
            _unitRepository = unitRepository;
            _userUnitRepository = userUnitRepository;
            _settingsRepository = settingsRepository;
            _guidGenerator = guidGenerator;
            _currentTenant = currentTenant;
        }

        /// <summary>
        /// Creates default main organizational unit for a new tenant
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="tenantCode">The tenant code (used for unit code)</param>
        /// <returns>The created organizational unit</returns>
        [UnitOfWork]
        public virtual async Task<OrganizationalUnit> CreateDefaultForTenantAsync(Guid? tenantId, string tenantCode)
        {
            Check.NotNullOrWhiteSpace(tenantCode, nameof(tenantCode));

            // Code: {tenantCode}-MAIN
            var code = $"{tenantCode.ToUpper()}-MAIN";

            // Create the unit
            var unitId = _guidGenerator.Create();
            var unit = new OrganizationalUnit(
                id: unitId,
                name: "Główna",
                code: code,
                tenantId: tenantId);

            await _unitRepository.InsertAsync(unit);

            // Create default settings
            var settings = new OrganizationalUnitSettings(
                id: _guidGenerator.Create(),
                organizationalUnitId: unitId,
                tenantId: tenantId,
                isMainUnit: true);

            await _settingsRepository.InsertAsync(settings);

            return unit;
        }

        /// <summary>
        /// Validates if user has access to organizational unit
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="organizationalUnitId">The organizational unit identifier</param>
        /// <param name="tenantId">The tenant identifier</param>
        /// <exception cref="UnauthorizedAccessException">Thrown when user doesn't have access</exception>
        public virtual async Task ValidateUserAccessAsync(Guid userId, Guid organizationalUnitId, Guid? tenantId)
        {
            var hasAccess = await _userUnitRepository.IsMemberAsync(
                tenantId,
                userId,
                organizationalUnitId);

            if (!hasAccess)
            {
                throw new UnauthorizedAccessException(
                    $"User '{userId}' does not have access to organizational unit '{organizationalUnitId}'");
            }
        }

        /// <summary>
        /// Gets all organizational units for a user
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="includeInactive">Include inactive units</param>
        /// <returns>List of organizational units</returns>
        public virtual async Task<List<OrganizationalUnit>> GetUserUnitsAsync(
            Guid userId,
            Guid? tenantId,
            bool includeInactive = false)
        {
            var unitIds = await _userUnitRepository.GetUserOrganizationalUnitIdsAsync(
                tenantId,
                userId);

            if (unitIds.Count == 0)
            {
                return new List<OrganizationalUnit>();
            }

            var units = await _unitRepository.GetListAsync(
                tenantId: tenantId,
                isActive: includeInactive ? null : true);

            return units
                .Where(u => unitIds.Contains(u.Id))
                .ToList();
        }

        /// <summary>
        /// Creates a new organizational unit
        /// </summary>
        /// <param name="name">Unit name</param>
        /// <param name="code">Unit code (must be unique per tenant)</param>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="address">Optional address</param>
        /// <param name="city">Optional city</param>
        /// <param name="postalCode">Optional postal code</param>
        /// <param name="email">Optional email</param>
        /// <param name="phone">Optional phone</param>
        /// <returns>The created organizational unit</returns>
        /// <exception cref="BusinessException">Thrown if code already exists</exception>
        public virtual async Task<OrganizationalUnit> CreateUnitAsync(
            string name,
            string code,
            Guid? tenantId,
            string? address = null,
            string? city = null,
            string? postalCode = null,
            string? email = null,
            string? phone = null)
        {
            Check.NotNullOrWhiteSpace(name, nameof(name));
            Check.NotNullOrWhiteSpace(code, nameof(code));

            // Validate code uniqueness
            var codeIsUnique = await _unitRepository.IsCodeUniqueAsync(tenantId, code);
            if (!codeIsUnique)
            {
                throw new BusinessException(
                    "OrganizationalUnit.CodeAlreadyExists",
                    $"Organizational unit with code '{code}' already exists for this tenant");
            }

            // Create the unit
            var unitId = _guidGenerator.Create();
            var unit = new OrganizationalUnit(
                id: unitId,
                name: name,
                code: code,
                tenantId: tenantId);

            // Set optional contact information
            if (!string.IsNullOrWhiteSpace(address) || !string.IsNullOrWhiteSpace(city) ||
                !string.IsNullOrWhiteSpace(postalCode) || !string.IsNullOrWhiteSpace(email) ||
                !string.IsNullOrWhiteSpace(phone))
            {
                unit.UpdateContactInfo(address, city, postalCode, email, phone);
            }

            // Save the unit
            await _unitRepository.InsertAsync(unit);

            // Create default settings
            var settings = new OrganizationalUnitSettings(
                id: _guidGenerator.Create(),
                organizationalUnitId: unitId,
                tenantId: tenantId);

            await _settingsRepository.InsertAsync(settings);

            return unit;
        }

        /// <summary>
        /// Assigns user to organizational unit
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="organizationalUnitId">The organizational unit identifier</param>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="roleId">Optional role identifier</param>
        /// <returns>The created user-unit assignment</returns>
        /// <exception cref="BusinessException">Thrown if user is already assigned</exception>
        public virtual async Task<UserOrganizationalUnit> AssignUserToUnitAsync(
            Guid userId,
            Guid organizationalUnitId,
            Guid? tenantId,
            Guid? roleId = null)
        {
            // Check if user is already assigned
            var existing = await _userUnitRepository.GetUserOrganizationalUnitIdsAsync(
                tenantId,
                userId);

            if (existing.Contains(organizationalUnitId))
            {
                throw new BusinessException(
                    "UserOrganizationalUnit.AlreadyAssigned",
                    "User is already assigned to this organizational unit");
            }

            // Create the assignment
            var id = _guidGenerator.Create();
            var assignment = new UserOrganizationalUnit(
                id: id,
                userId: userId,
                organizationalUnitId: organizationalUnitId,
                roleId: roleId,
                tenantId: tenantId);

            await _userUnitRepository.InsertAsync(assignment);

            return assignment;
        }

        /// <summary>
        /// Removes user from organizational unit
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="organizationalUnitId">The organizational unit identifier</param>
        /// <param name="tenantId">The tenant identifier</param>
        [UnitOfWork]
        public virtual async Task RemoveUserFromUnitAsync(
            Guid userId,
            Guid organizationalUnitId,
            Guid? tenantId)
        {
            await _userUnitRepository.RemoveUserFromUnitAsync(
                tenantId,
                userId,
                organizationalUnitId);
        }

        /// <summary>
        /// Updates organizational unit settings
        /// </summary>
        /// <param name="organizationalUnitId">The organizational unit identifier</param>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="currency">Optional currency to update</param>
        /// <param name="logoUrl">Optional logo URL</param>
        /// <param name="bannerText">Optional banner text</param>
        /// <returns>Updated settings</returns>
        public virtual async Task<OrganizationalUnitSettings> UpdateUnitSettingsAsync(
            Guid organizationalUnitId,
            Guid? tenantId,
            string? currency = null,
            string? logoUrl = null,
            string? bannerText = null)
        {
            var settings = await _settingsRepository.GetOrCreateAsync(tenantId, organizationalUnitId);

            if (!string.IsNullOrWhiteSpace(currency))
            {
                settings.UpdateCurrency(currency);
            }

            if (logoUrl != null || bannerText != null)
            {
                settings.UpdateBranding(logoUrl, bannerText);
            }

            await _settingsRepository.UpdateAsync(settings);

            return settings;
        }

        /// <summary>
        /// Gets organizational unit settings
        /// </summary>
        /// <param name="organizationalUnitId">The organizational unit identifier</param>
        /// <param name="tenantId">The tenant identifier</param>
        /// <returns>Settings or null if not found</returns>
        public virtual async Task<OrganizationalUnitSettings?> GetUnitSettingsAsync(
            Guid organizationalUnitId,
            Guid? tenantId)
        {
            return await _settingsRepository.GetByOrganizationalUnitAsync(tenantId, organizationalUnitId);
        }

        /// <summary>
        /// Checks if organizational unit exists
        /// </summary>
        /// <param name="organizationalUnitId">The organizational unit identifier</param>
        /// <returns>True if exists</returns>
        public virtual async Task<bool> UnitExistsAsync(Guid organizationalUnitId)
        {
            var unit = await _unitRepository.GetAsync(organizationalUnitId);
            return unit != null;
        }

        /// <summary>
        /// Gets member count for organizational unit
        /// </summary>
        /// <param name="organizationalUnitId">The organizational unit identifier</param>
        /// <param name="tenantId">The tenant identifier</param>
        /// <returns>Number of active members</returns>
        public virtual async Task<long> GetUnitMemberCountAsync(
            Guid organizationalUnitId,
            Guid? tenantId)
        {
            return await _userUnitRepository.GetOrganizationalUnitMemberCountAsync(tenantId, organizationalUnitId);
        }
    }
}
