using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MP.Domain.FloorPlans;
using MP.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace MP.EntityFrameworkCore.FloorPlans
{
    public class EfCoreFloorPlanElementRepository : EfCoreRepository<MPDbContext, FloorPlanElement, Guid>, IFloorPlanElementRepository
    {
        public EfCoreFloorPlanElementRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<List<FloorPlanElement>> GetListByFloorPlanAsync(
            Guid floorPlanId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();

            return await dbContext.FloorPlanElements
                .Where(x => x.FloorPlanId == floorPlanId)
                .OrderBy(x => x.ElementType)
                .ThenBy(x => x.X)
                .ThenBy(x => x.Y)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<FloorPlanElement>> GetListByFloorPlanAndTypeAsync(
            Guid floorPlanId,
            FloorPlanElementType elementType,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();

            return await dbContext.FloorPlanElements
                .Where(x => x.FloorPlanId == floorPlanId && x.ElementType == elementType)
                .OrderBy(x => x.X)
                .ThenBy(x => x.Y)
                .ToListAsync(cancellationToken);
        }

        public async Task DeleteByFloorPlanAsync(
            Guid floorPlanId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();

            await dbContext.FloorPlanElements
                .Where(x => x.FloorPlanId == floorPlanId)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
