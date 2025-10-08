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
    [Route("api/app/floor-plan")]
    public class FloorPlanController : AbpControllerBase, IFloorPlanAppService
    {
        private readonly IFloorPlanAppService _floorPlanAppService;

        public FloorPlanController(IFloorPlanAppService floorPlanAppService)
        {
            _floorPlanAppService = floorPlanAppService;
        }

        [HttpGet]
        [Route("{id}")]
        public Task<FloorPlanDto> GetAsync(Guid id)
        {
            return _floorPlanAppService.GetAsync(id);
        }

        [HttpGet]
        public Task<PagedResultDto<FloorPlanDto>> GetListAsync(GetFloorPlanListDto input)
        {
            return _floorPlanAppService.GetListAsync(input);
        }

        [HttpGet]
        [Route("by-tenant")]
        public Task<List<FloorPlanDto>> GetListByTenantAsync(Guid? tenantId, bool? isActive = null)
        {
            return _floorPlanAppService.GetListByTenantAsync(tenantId, isActive);
        }

        [HttpPost]
        public Task<FloorPlanDto> CreateAsync(CreateFloorPlanDto input)
        {
            return _floorPlanAppService.CreateAsync(input);
        }

        [HttpPut]
        [Route("{id}")]
        public Task<FloorPlanDto> UpdateAsync(Guid id, UpdateFloorPlanDto input)
        {
            return _floorPlanAppService.UpdateAsync(id, input);
        }

        [HttpDelete]
        [Route("{id}")]
        public Task DeleteAsync(Guid id)
        {
            return _floorPlanAppService.DeleteAsync(id);
        }

        [HttpPost]
        [Route("{id}/publish")]
        public Task<FloorPlanDto> PublishAsync(Guid id)
        {
            return _floorPlanAppService.PublishAsync(id);
        }

        [HttpPost]
        [Route("{id}/deactivate")]
        public Task<FloorPlanDto> DeactivateAsync(Guid id)
        {
            return _floorPlanAppService.DeactivateAsync(id);
        }

        [HttpGet]
        [Route("{floorPlanId}/booths")]
        public Task<List<FloorPlanBoothDto>> GetBoothsAsync(Guid floorPlanId)
        {
            return _floorPlanAppService.GetBoothsAsync(floorPlanId);
        }

        [HttpPost]
        [Route("{floorPlanId}/booths")]
        public Task<FloorPlanBoothDto> AddBoothAsync(Guid floorPlanId, CreateFloorPlanBoothDto input)
        {
            return _floorPlanAppService.AddBoothAsync(floorPlanId, input);
        }

        [HttpPut]
        [Route("{floorPlanId}/booths/{boothId}")]
        public Task<FloorPlanBoothDto> UpdateBoothPositionAsync(Guid floorPlanId, Guid boothId, CreateFloorPlanBoothDto input)
        {
            return _floorPlanAppService.UpdateBoothPositionAsync(floorPlanId, boothId, input);
        }

        [HttpDelete]
        [Route("{floorPlanId}/booths/{boothId}")]
        public Task RemoveBoothAsync(Guid floorPlanId, Guid boothId)
        {
            return _floorPlanAppService.RemoveBoothAsync(floorPlanId, boothId);
        }

        [HttpGet]
        [Route("{floorPlanId}/booths-availability")]
        public Task<List<BoothAvailabilityDto>> GetBoothsAvailabilityAsync(Guid floorPlanId, DateTime startDate, DateTime endDate)
        {
            return _floorPlanAppService.GetBoothsAvailabilityAsync(floorPlanId, startDate, endDate);
        }
    }
}