using Microsoft.EntityFrameworkCore;
using MP.Domain.Settlements;
using MP.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace MP.Settlements
{
    public class EfCoreSettlementRepository : EfCoreRepository<MPDbContext, Settlement, Guid>, ISettlementRepository
    {
        public EfCoreSettlementRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<List<Settlement>> GetUserSettlementsAsync(
            Guid userId,
            SettlementStatus? status = null,
            DateTime? createdAfter = null,
            DateTime? createdBefore = null,
            int skipCount = 0,
            int maxResultCount = 10,
            string? sorting = null,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var query = dbContext.Settlements
                .Where(s => s.UserId == userId);

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            if (createdAfter.HasValue)
            {
                query = query.Where(s => s.CreationTime >= createdAfter.Value);
            }

            if (createdBefore.HasValue)
            {
                query = query.Where(s => s.CreationTime <= createdBefore.Value);
            }

            // Apply sorting
            if (!string.IsNullOrWhiteSpace(sorting))
            {
                // Simple sorting implementation
                if (sorting.Contains("desc", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.OrderByDescending(s => s.CreationTime);
                }
                else
                {
                    query = query.OrderBy(s => s.CreationTime);
                }
            }
            else
            {
                query = query.OrderByDescending(s => s.CreationTime);
            }

            return await query
                .Skip(skipCount)
                .Take(maxResultCount)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetUserSettlementsCountAsync(
            Guid userId,
            SettlementStatus? status = null,
            DateTime? createdAfter = null,
            DateTime? createdBefore = null,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var query = dbContext.Settlements
                .Where(s => s.UserId == userId);

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            if (createdAfter.HasValue)
            {
                query = query.Where(s => s.CreationTime >= createdAfter.Value);
            }

            if (createdBefore.HasValue)
            {
                query = query.Where(s => s.CreationTime <= createdBefore.Value);
            }

            return await query.CountAsync(cancellationToken);
        }

        public async Task<Settlement?> FindBySettlementNumberAsync(
            string settlementNumber,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Settlements
                .FirstOrDefaultAsync(s => s.SettlementNumber == settlementNumber, cancellationToken);
        }

        public async Task<List<Settlement>> GetPendingSettlementsAsync(
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Settlements
                .Where(s => s.Status == SettlementStatus.Pending)
                .OrderBy(s => s.CreationTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<decimal> GetUserTotalEarningsAsync(
            Guid userId,
            SettlementStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var query = dbContext.Settlements
                .Where(s => s.UserId == userId);

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            return await query.SumAsync(s => s.NetAmount, cancellationToken);
        }
    }
}
