using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MP.OrganizationalUnits;
using MP.OrganizationalUnits.Dtos;
using MP.Permissions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;

namespace MP.Controllers
{
    
    [Area("app")]
    [Route("api/app/organizational-units")]
    [Authorize]
    public class UserOrganizationalUnitsController : AbpControllerBase
    {
        private readonly IUserOrganizationalUnitAppService _appService;

        public UserOrganizationalUnitsController(IUserOrganizationalUnitAppService appService)
        {
            _appService = appService;
        }

        /// <summary>
        /// Gets current user's organizational units
        /// GET /api/app/organizational-units/my-units
        /// </summary>
        [HttpGet]
        [Route("my-units")]
        public Task<List<MyUnitDto>> GetMyUnitsAsync()
        {
            return _appService.GetMyUnitsAsync();
        }

        /// <summary>
        /// Lists all users in a specific organizational unit
        /// GET /api/app/organizational-units/{unitId}/users
        /// </summary>
        [HttpGet]
        [Route("{unitId}/users")]
        [Authorize(MPPermissions.OrganizationalUnits.ManageUsers)]
        public Task<List<UserInUnitDto>> GetUsersInUnitAsync(Guid unitId)
        {
            return _appService.GetUsersInUnitAsync(unitId);
        }

        /// <summary>
        /// Assigns a user to an organizational unit
        /// POST /api/app/organizational-units/{unitId}/users
        /// </summary>
        [HttpPost]
        [Route("{unitId}/users")]
        [Authorize(MPPermissions.OrganizationalUnits.ManageUsers)]
        public Task<UserInUnitDto> AssignUserToUnitAsync(Guid unitId, AssignUserDto input)
        {
            return _appService.AssignUserToUnitAsync(unitId, input);
        }

        /// <summary>
        /// Removes a user from an organizational unit
        /// DELETE /api/app/organizational-units/{unitId}/users/{userId}
        /// </summary>
        [HttpDelete]
        [Route("{unitId}/users/{userId}")]
        [Authorize(MPPermissions.OrganizationalUnits.ManageUsers)]
        public Task RemoveUserFromUnitAsync(Guid unitId, Guid userId)
        {
            return _appService.RemoveUserFromUnitAsync(unitId, userId);
        }

        /// <summary>
        /// Updates a user's role within an organizational unit
        /// PUT /api/app/organizational-units/{unitId}/users/{userId}/role
        /// </summary>
        [HttpPut]
        [Route("{unitId}/users/{userId}/role")]
        [Authorize(MPPermissions.OrganizationalUnits.ManageUsers)]
        public Task<UserInUnitDto> UpdateUserRoleAsync(Guid unitId, Guid userId, UpdateUserRoleDto input)
        {
            return _appService.UpdateUserRoleAsync(unitId, userId, input);
        }

        /// <summary>
        /// Joins user to an organizational unit using registration code
        /// POST /api/app/organizational-units/join
        /// </summary>
        [HttpPost]
        [Route("join")]
        public Task<JoinUnitResultDto> JoinUnitWithCodeAsync(JoinUnitDto input)
        {
            return _appService.JoinUnitWithCodeAsync(input.Code);
        }
    }
}
