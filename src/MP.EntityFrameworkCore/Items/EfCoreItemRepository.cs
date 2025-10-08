using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MP.Domain.Items;
using MP.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace MP.EntityFrameworkCore.Items
{
    public class EfCoreItemRepository : EfCoreRepository<MPDbContext, Item, Guid>, IItemRepository
    {
        public EfCoreItemRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<List<Item>> GetListByUserIdAsync(
            Guid userId,
            int skipCount,
            int maxResultCount,
            ItemStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            var query = dbSet
                .AsNoTracking()
                .Where(x => x.UserId == userId);

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            return await query
                .OrderByDescending(x => x.CreationTime)
                .Skip(skipCount)
                .Take(maxResultCount)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Item>> GetListByIdsAsync(
            List<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .AsNoTracking()
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetCountByUserIdAsync(
            Guid userId,
            ItemStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            var query = dbSet
                .AsNoTracking()
                .Where(x => x.UserId == userId);

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            return await query.CountAsync(cancellationToken);
        }
    }
}
