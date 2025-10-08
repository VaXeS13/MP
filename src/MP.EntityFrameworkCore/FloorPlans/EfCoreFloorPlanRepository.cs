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
    public class EfCoreFloorPlanRepository : EfCoreRepository<MPDbContext, FloorPlan, Guid>, IFloorPlanRepository
    {
        public EfCoreFloorPlanRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<List<FloorPlan>> GetListByTenantAsync(
            Guid? tenantId,
            bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            var queryable = await GetQueryableAsync();

            queryable = queryable
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId);

            if (isActive.HasValue)
            {
                queryable = queryable.Where(x => x.IsActive == isActive.Value);
            }

            return await queryable
                .OrderBy(x => x.Level)
                .ThenBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<FloorPlan?> FindByNameAsync(
            string name,
            Guid? tenantId,
            CancellationToken cancellationToken = default)
        {
            var queryable = await GetQueryableAsync();

            return await queryable
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Name == name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(
            string name,
            Guid? tenantId,
            Guid? excludeId = null,
            CancellationToken cancellationToken = default)
        {
            var queryable = await GetQueryableAsync();

            queryable = queryable
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Name == name);

            if (excludeId.HasValue)
            {
                queryable = queryable.Where(x => x.Id != excludeId.Value);
            }

            return await queryable.AnyAsync(cancellationToken);
        }
    }
}