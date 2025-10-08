using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MP.FloorPlans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace MP.Controllers
{
    [RemoteService(Name = "Default")]
    [Area("app")]
    [Route("api/app/floor-plan-element")]
    public class FloorPlanElementController : AbpControllerBase, IFloorPlanElementAppService
    {
        private readonly IFloorPlanElementAppService _floorPlanElementAppService;

        public FloorPlanElementController(IFloorPlanElementAppService floorPlanElementAppService)
        {
            _floorPlanElementAppService = floorPlanElementAppService;
        }

        [HttpGet]
        [Route("{id}")]
        public Task<FloorPlanElementDto> GetAsync(Guid id)
        {
            return _floorPlanElementAppService.GetAsync(id);
        }

        [HttpGet]
        public Task<PagedResultDto<FloorPlanElementDto>> GetListAsync(GetFloorPlanElementListDto input)
        {
            return _floorPlanElementAppService.GetListAsync(input);
        }

        [HttpGet]
        [Route("by-floor-plan/{floorPlanId}")]
        public Task<List<FloorPlanElementDto>> GetListByFloorPlanAsync(Guid floorPlanId)
        {
            return _floorPlanElementAppService.GetListByFloorPlanAsync(floorPlanId);
        }

        [HttpPost]
        [Route("floor-plan/{floorPlanId}")]
        public Task<FloorPlanElementDto> CreateAsync(Guid floorPlanId, CreateFloorPlanElementDto input)
        {
            return _floorPlanElementAppService.CreateAsync(floorPlanId, input);
        }

        [HttpPut]
        [Route("{id}")]
        public Task<FloorPlanElementDto> UpdateAsync(Guid id, UpdateFloorPlanElementDto input)
        {
            return _floorPlanElementAppService.UpdateAsync(id, input);
        }

        [HttpDelete]
        [Route("{id}")]
        public Task DeleteAsync(Guid id)
        {
            return _floorPlanElementAppService.DeleteAsync(id);
        }

        [HttpDelete]
        [Route("by-floor-plan/{floorPlanId}")]
        public Task DeleteByFloorPlanAsync(Guid floorPlanId)
        {
            return _floorPlanElementAppService.DeleteByFloorPlanAsync(floorPlanId);
        }
    }
}
