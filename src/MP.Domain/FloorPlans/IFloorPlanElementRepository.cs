using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.FloorPlans
{
    public interface IFloorPlanElementRepository : IRepository<FloorPlanElement, Guid>
    {
        Task<List<FloorPlanElement>> GetListByFloorPlanAsync(
            Guid floorPlanId,
            CancellationToken cancellationToken = default);

        Task<List<FloorPlanElement>> GetListByFloorPlanAndTypeAsync(
            Guid floorPlanId,
            FloorPlanElementType elementType,
            CancellationToken cancellationToken = default);

        Task DeleteByFloorPlanAsync(
            Guid floorPlanId,
            CancellationToken cancellationToken = default);
    }
}
