using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MP.Application.Contracts.BoothTypes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MP.Controllers
{
    [ApiController]
    [Route("api/app/booth-type")]
    [Authorize]
    public class BoothTypeController : ControllerBase
    {
        private readonly IBoothTypeAppService _boothTypeAppService;

        public BoothTypeController(IBoothTypeAppService boothTypeAppService)
        {
            _boothTypeAppService = boothTypeAppService;
        }

        /// <summary>
        /// Get all active booth types for current tenant
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<List<BoothTypeDto>>> GetActiveTypes()
        {
            try
            {
                var result = await _boothTypeAppService.GetActiveTypesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
