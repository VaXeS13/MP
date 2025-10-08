using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MP.FloorPlans
{
    public interface IFloorPlanElementAppService : IApplicationService
    {
        Task<FloorPlanElementDto> GetAsync(Guid id);

        Task<PagedResultDto<FloorPlanElementDto>> GetListAsync(GetFloorPlanElementListDto input);

        Task<List<FloorPlanElementDto>> GetListByFloorPlanAsync(Guid floorPlanId);

        Task<FloorPlanElementDto> CreateAsync(Guid floorPlanId, CreateFloorPlanElementDto input);

        Task<FloorPlanElementDto> UpdateAsync(Guid id, UpdateFloorPlanElementDto input);

        Task DeleteAsync(Guid id);

        Task DeleteByFloorPlanAsync(Guid floorPlanId);
    }
}
