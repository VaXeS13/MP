using MP.OrganizationalUnits.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MP.OrganizationalUnits
{
    public interface IUserOrganizationalUnitAppService : IApplicationService
    {
        /// <summary>
        /// Gets current user's organizational units with details
        /// </summary>
        Task<List<MyUnitDto>> GetMyUnitsAsync();

        /// <summary>
        /// Switches current user's active organizational unit
        /// </summary>
        Task<SwitchUnitDto> SwitchUnitAsync(Guid unitId);

        /// <summary>
        /// Gets all users assigned to a specific organizational unit
        /// </summary>
        Task<List<UserInUnitDto>> GetUsersInUnitAsync(Guid unitId);

        /// <summary>
        /// Assigns a user to an organizational unit
        /// </summary>
        Task<UserInUnitDto> AssignUserToUnitAsync(Guid unitId, AssignUserDto input);

        /// <summary>
        /// Removes a user from an organizational unit
        /// </summary>
        Task RemoveUserFromUnitAsync(Guid unitId, Guid userId);

        /// <summary>
        /// Updates a user's role within an organizational unit
        /// </summary>
        Task<UserInUnitDto> UpdateUserRoleAsync(Guid unitId, Guid userId, UpdateUserRoleDto input);

        /// <summary>
        /// Joins authenticated user to an organizational unit using a registration code
        /// </summary>
        Task<JoinUnitResultDto> JoinUnitWithCodeAsync(string code);
    }
}
