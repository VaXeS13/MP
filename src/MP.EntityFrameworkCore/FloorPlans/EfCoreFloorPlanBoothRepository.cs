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
    public class EfCoreFloorPlanBoothRepository : EfCoreRepository<MPDbContext, FloorPlanBooth, Guid>, IFloorPlanBoothRepository
    {
        public EfCoreFloorPlanBoothRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<List<FloorPlanBooth>> GetListByFloorPlanAsync(
            Guid floorPlanId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();

            return await dbContext.FloorPlanBooths
                .Include(x => x.Booth)
                .Where(x => x.FloorPlanId == floorPlanId)
                .OrderBy(x => x.X)
                .ThenBy(x => x.Y)
                .ToListAsync(cancellationToken);
        }

        public async Task<FloorPlanBooth?> FindByFloorPlanAndBoothAsync(
            Guid floorPlanId,
            Guid boothId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();

            return await dbContext.FloorPlanBooths
                .Include(x => x.Booth)
                .Where(x => x.FloorPlanId == floorPlanId && x.BoothId == boothId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> ExistsForBoothAsync(
            Guid boothId,
            Guid? excludeFloorPlanId = null,
            CancellationToken cancellationToken = default)
        {
            var queryable = await GetQueryableAsync();

            queryable = queryable.Where(x => x.BoothId == boothId);

            if (excludeFloorPlanId.HasValue)
            {
                queryable = queryable.Where(x => x.FloorPlanId != excludeFloorPlanId.Value);
            }

            return await queryable.AnyAsync(cancellationToken);
        }

        public async Task DeleteByFloorPlanAsync(
            Guid floorPlanId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();

            await dbContext.FloorPlanBooths
                .Where(x => x.FloorPlanId == floorPlanId)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}