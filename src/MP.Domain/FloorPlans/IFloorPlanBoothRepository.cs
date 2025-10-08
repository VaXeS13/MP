using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.FloorPlans
{
    public interface IFloorPlanBoothRepository : IRepository<FloorPlanBooth, Guid>
    {
        Task<List<FloorPlanBooth>> GetListByFloorPlanAsync(
            Guid floorPlanId,
            CancellationToken cancellationToken = default);

        Task<FloorPlanBooth?> FindByFloorPlanAndBoothAsync(
            Guid floorPlanId,
            Guid boothId,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsForBoothAsync(
            Guid boothId,
            Guid? excludeFloorPlanId = null,
            CancellationToken cancellationToken = default);

        Task DeleteByFloorPlanAsync(
            Guid floorPlanId,
            CancellationToken cancellationToken = default);
    }
}