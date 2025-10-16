using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MP.FloorPlans;
using MP.Permissions;
using Volo.Abp.Application.Dtos;
using MP.Domain.FloorPlans;

namespace MP.Controllers
{
    [ApiController]
    [Route("api/app/floor-plan")]
    [Authorize(MPPermissions.FloorPlans.Default)]
    public class FloorPlanController : ControllerBase
    {
        private readonly IFloorPlanAppService _floorPlanAppService;

        public FloorPlanController(IFloorPlanAppService floorPlanAppService)
        {
            _floorPlanAppService = floorPlanAppService;
        }

        [HttpGet]
        public async Task<PagedResultDto<FloorPlanDto>> GetList([FromQuery] GetFloorPlanListDto input)
        {
            return await _floorPlanAppService.GetListAsync(input);
        }

        [HttpGet("{id}")]
        public async Task<FloorPlanDto> Get(Guid id)
        {
            return await _floorPlanAppService.GetAsync(id);
        }

        [HttpGet("{floorPlanId}/booths")]
        public async Task<List<FloorPlanBoothDto>> GetBooths(Guid floorPlanId)
        {
            return await _floorPlanAppService.GetBoothsAsync(floorPlanId);
        }

        [HttpGet("{floorPlanId}/booths-availability")]
        public async Task<List<BoothAvailabilityDto>> GetBoothsAvailability(
            Guid floorPlanId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            return await _floorPlanAppService.GetBoothsAvailabilityAsync(floorPlanId, startDate, endDate);
        }
    }
}
