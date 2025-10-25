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
    public class OrganizationalUnitsController : AbpControllerBase, IOrganizationalUnitAppService
    {
        private readonly IOrganizationalUnitAppService _appService;

        public OrganizationalUnitsController(IOrganizationalUnitAppService appService)
        {
            _appService = appService;
        }

        [HttpGet]
        [Route("{id}")]
        public Task<OrganizationalUnitDto> GetAsync(Guid id)
        {
            return _appService.GetAsync(id);
        }

        [HttpGet]
        public Task<List<OrganizationalUnitDto>> GetListAsync()
        {
            return _appService.GetListAsync();
        }

        [HttpPost]
        [Authorize(MPPermissions.OrganizationalUnits.Create)]
        public Task<OrganizationalUnitDto> CreateAsync(CreateUpdateOrganizationalUnitDto input)
        {
            return _appService.CreateAsync(input);
        }

        [HttpPut]
        [Route("{id}")]
        [Authorize(MPPermissions.OrganizationalUnits.Edit)]
        public Task<OrganizationalUnitDto> UpdateAsync(Guid id, CreateUpdateOrganizationalUnitDto input)
        {
            return _appService.UpdateAsync(id, input);
        }

        [HttpDelete]
        [Route("{id}")]
        [Authorize(MPPermissions.OrganizationalUnits.Delete)]
        public Task DeleteAsync(Guid id)
        {
            return _appService.DeleteAsync(id);
        }

        [HttpGet]
        [Route("{unitId}/settings")]
        public Task<OrganizationalUnitSettingsDto> GetSettingsAsync(Guid unitId)
        {
            return _appService.GetSettingsAsync(unitId);
        }

        [HttpPut]
        [Route("{unitId}/settings")]
        [Authorize(MPPermissions.OrganizationalUnits.Edit)]
        public Task<OrganizationalUnitSettingsDto> UpdateSettingsAsync(Guid unitId, UpdateUnitSettingsDto input)
        {
            return _appService.UpdateSettingsAsync(unitId, input);
        }

        [HttpGet]
        [Route("current")]
        public Task<CurrentUnitDto> GetCurrentUnitAsync()
        {
            return _appService.GetCurrentUnitAsync();
        }

        [HttpPost]
        [Route("switch")]
[HttpPost]        [Route("switch")]        public Task<SwitchUnitDto> SwitchUnitAsync(Guid unitId)        {            return _appService.SwitchUnitAsync(unitId);        }
    }
}
