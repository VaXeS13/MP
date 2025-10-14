using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MP.Domain.Promotions;
using MP.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace MP.Promotions
{
    public class PromotionRepository : EfCoreRepository<MPDbContext, Promotion, Guid>, IPromotionRepository
    {
        public PromotionRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<List<Promotion>> GetActivePromotionsAsync(CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            var now = DateTime.UtcNow;

            return await dbSet
                .Where(p => p.IsActive)
                .Where(p => !p.ValidFrom.HasValue || p.ValidFrom.Value <= now)
                .Where(p => !p.ValidTo.HasValue || p.ValidTo.Value >= now)
                .Where(p => !p.MaxUsageCount.HasValue || p.CurrentUsageCount < p.MaxUsageCount.Value)
                .OrderByDescending(p => p.Priority)
                .ThenBy(p => p.CreationTime)
                .ToListAsync(GetCancellationToken(cancellationToken));
        }

        public async Task<Promotion?> GetByPromoCodeAsync(string promoCode, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(promoCode))
                return null;

            var dbSet = await GetDbSetAsync();
            var normalizedCode = promoCode.Trim().ToUpperInvariant();

            return await dbSet
                .FirstOrDefaultAsync(
                    p => p.PromoCode != null && p.PromoCode == normalizedCode,
                    GetCancellationToken(cancellationToken));
        }

        public async Task<List<Promotion>> GetListAsync(
            string? filterText = null,
            bool? isActive = null,
            PromotionType? type = null,
            int skipCount = 0,
            int maxResultCount = int.MaxValue,
            string sorting = "Priority DESC, CreationTime DESC",
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            var query = ApplyFilters(dbSet, filterText, isActive, type);

            return await query
                .OrderBy(sorting)
                .Skip(skipCount)
                .Take(maxResultCount)
                .ToListAsync(GetCancellationToken(cancellationToken));
        }

        public async Task<long> GetCountAsync(
            string? filterText = null,
            bool? isActive = null,
            PromotionType? type = null,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            var query = ApplyFilters(dbSet, filterText, isActive, type);

            return await query.LongCountAsync(GetCancellationToken(cancellationToken));
        }

        public async Task<bool> PromoCodeExistsAsync(
            string promoCode,
            Guid? excludeId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(promoCode))
                return false;

            var dbSet = await GetDbSetAsync();
            var normalizedCode = promoCode.Trim().ToUpperInvariant();

            var query = dbSet.Where(p => p.PromoCode != null && p.PromoCode == normalizedCode);

            if (excludeId.HasValue)
            {
                query = query.Where(p => p.Id != excludeId.Value);
            }

            return await query.AnyAsync(GetCancellationToken(cancellationToken));
        }

        private IQueryable<Promotion> ApplyFilters(
            IQueryable<Promotion> query,
            string? filterText,
            bool? isActive,
            PromotionType? type)
        {
            if (!string.IsNullOrWhiteSpace(filterText))
            {
                query = query.Where(p =>
                    p.Name.Contains(filterText) ||
                    (p.Description != null && p.Description.Contains(filterText)) ||
                    (p.PromoCode != null && p.PromoCode.Contains(filterText)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(p => p.Type == type.Value);
            }

            return query;
        }
    }
}
