using MP.OrganizationalUnits.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MP.OrganizationalUnits
{
    public interface IOrganizationalUnitAppService : IApplicationService
    {
        /// <summary>
        /// Gets a single organizational unit by ID
        /// </summary>
        Task<OrganizationalUnitDto> GetAsync(Guid id);

        /// <summary>
        /// Lists all organizational units in the current tenant
        /// </summary>
        Task<List<OrganizationalUnitDto>> GetListAsync();

        /// <summary>
        /// Creates a new organizational unit
        /// </summary>
        Task<OrganizationalUnitDto> CreateAsync(CreateUpdateOrganizationalUnitDto input);

        /// <summary>
        /// Updates an existing organizational unit
        /// </summary>
        Task<OrganizationalUnitDto> UpdateAsync(Guid id, CreateUpdateOrganizationalUnitDto input);

        /// <summary>
        /// Deletes an organizational unit (soft delete)
        /// </summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Gets or creates the current user's default unit
        /// </summary>
        Task<CurrentUnitDto> GetCurrentUnitAsync();

        /// <summary>
        /// Switches the current user's active unit
        /// </summary>
        Task<SwitchUnitDto> SwitchUnitAsync(Guid unitId);

        /// <summary>
        /// Updates unit settings (currency, payment providers, etc.)
        /// </summary>
        Task<OrganizationalUnitSettingsDto> UpdateSettingsAsync(Guid unitId, UpdateUnitSettingsDto input);

        /// <summary>
        /// Gets unit settings
        /// </summary>
        Task<OrganizationalUnitSettingsDto> GetSettingsAsync(Guid unitId);
    }
}
