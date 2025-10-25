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
    [Route("api/app/registration-codes")]
    [Authorize]
    public class RegistrationCodesController : AbpControllerBase
    {
        private readonly IRegistrationCodeAppService _appService;

        public RegistrationCodesController(IRegistrationCodeAppService appService)
        {
            _appService = appService;
        }

        /// <summary>
        /// Generates a new registration code for an organizational unit
        /// POST /api/app/registration-codes/generate
        /// </summary>
        [HttpPost]
        [Route("generate")]
        [Authorize(MPPermissions.OrganizationalUnits.ManageUsers)]
        public Task<RegistrationCodeDto> GenerateCodeAsync(GenerateRegistrationCodeRequestDto input)
        {
            return _appService.GenerateCodeAsync(input.OrganizationalUnitId, input.CreateDto);
        }

        /// <summary>
        /// Joins user to organization unit using a registration code
        /// POST /api/app/registration-codes/join
        /// </summary>
        [HttpPost]
        [Route("join")]
        public Task<JoinUnitResultDto> JoinUnitWithCodeAsync(JoinUnitDto input)
        {
            return _appService.JoinUnitWithCodeAsync(input.Code);
        }

        /// <summary>
        /// Validates if a registration code is valid and active
        /// GET /api/app/registration-codes/validate?code={code}
        /// </summary>
        [HttpGet]
        [Route("validate")]
        [AllowAnonymous]
        public Task<ValidateCodeResultDto> ValidateCodeAsync([FromQuery] string code)
        {
            return _appService.ValidateCodeAsync(code);
        }

        /// <summary>
        /// Deactivates a registration code
        /// DELETE /api/app/registration-codes/{codeId}
        /// </summary>
        [HttpDelete]
        [Route("{codeId}")]
        [Authorize(MPPermissions.OrganizationalUnits.ManageUsers)]
        public Task DeactivateCodeAsync(Guid codeId)
        {
            return _appService.DeactivateCodeAsync(codeId);
        }

        /// <summary>
        /// Lists all registration codes for an organizational unit
        /// GET /api/app/registration-codes/by-unit/{unitId}
        /// </summary>
        [HttpGet]
        [Route("by-unit/{unitId}")]
        [Authorize(MPPermissions.OrganizationalUnits.ManageUsers)]
        public Task<List<RegistrationCodeDto>> ListCodesAsync(Guid unitId)
        {
            return _appService.ListCodesAsync(unitId);
        }
    }
}
