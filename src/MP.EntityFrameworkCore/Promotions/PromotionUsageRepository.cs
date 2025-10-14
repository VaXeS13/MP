using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MP.Domain.Promotions;
using MP.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace MP.Promotions
{
    public class PromotionUsageRepository : EfCoreRepository<MPDbContext, PromotionUsage, Guid>, IPromotionUsageRepository
    {
        public PromotionUsageRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<int> GetUsageCountByUserAsync(
            Guid promotionId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .Where(u => u.PromotionId == promotionId && u.UserId == userId)
                .CountAsync(GetCancellationToken(cancellationToken));
        }

        public async Task<List<PromotionUsage>> GetByPromotionIdAsync(
            Guid promotionId,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .Where(u => u.PromotionId == promotionId)
                .OrderByDescending(u => u.CreationTime)
                .ToListAsync(GetCancellationToken(cancellationToken));
        }

        public async Task<List<PromotionUsage>> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .Include(u => u.Promotion)
                .Where(u => u.UserId == userId)
                .OrderByDescending(u => u.CreationTime)
                .ToListAsync(GetCancellationToken(cancellationToken));
        }

        public async Task<bool> HasUserUsedPromotionAsync(
            Guid promotionId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .AnyAsync(
                    u => u.PromotionId == promotionId && u.UserId == userId,
                    GetCancellationToken(cancellationToken));
        }
    }
}
