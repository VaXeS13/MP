using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.FloorPlans
{
    public interface IFloorPlanRepository : IRepository<FloorPlan, Guid>
    {
        Task<List<FloorPlan>> GetListByTenantAsync(
            Guid? tenantId,
            bool? isActive = null,
            CancellationToken cancellationToken = default);

        Task<FloorPlan?> FindByNameAsync(
            string name,
            Guid? tenantId,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(
            string name,
            Guid? tenantId,
            Guid? excludeId = null,
            CancellationToken cancellationToken = default);
    }
}