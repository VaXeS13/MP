using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MP.FloorPlans
{
    public interface IFloorPlanAppService : IApplicationService
    {
        Task<FloorPlanDto> GetAsync(Guid id);

        Task<PagedResultDto<FloorPlanDto>> GetListAsync(GetFloorPlanListDto input);

        Task<List<FloorPlanDto>> GetListByTenantAsync(Guid? tenantId, bool? isActive = null);

        Task<FloorPlanDto> CreateAsync(CreateFloorPlanDto input);

        Task<FloorPlanDto> UpdateAsync(Guid id, UpdateFloorPlanDto input);

        Task DeleteAsync(Guid id);

        Task<FloorPlanDto> PublishAsync(Guid id);

        Task<FloorPlanDto> DeactivateAsync(Guid id);

        Task<List<FloorPlanBoothDto>> GetBoothsAsync(Guid floorPlanId);

        Task<FloorPlanBoothDto> AddBoothAsync(Guid floorPlanId, CreateFloorPlanBoothDto input);

        Task<FloorPlanBoothDto> UpdateBoothPositionAsync(Guid floorPlanId, Guid boothId, CreateFloorPlanBoothDto input);

        Task RemoveBoothAsync(Guid floorPlanId, Guid boothId);

        Task<List<BoothAvailabilityDto>> GetBoothsAvailabilityAsync(Guid floorPlanId, DateTime startDate, DateTime endDate);
    }
}