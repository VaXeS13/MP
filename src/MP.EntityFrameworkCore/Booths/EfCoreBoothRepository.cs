using Microsoft.EntityFrameworkCore;
using MP.Domain.Booths;
using MP.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using MP.Rentals;

namespace MP.Booths
{
    public class EfCoreBoothRepository : EfCoreRepository<MPDbContext, Booth, Guid>, IBoothRepository
    {
        public EfCoreBoothRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public override async Task<IQueryable<Booth>> WithDetailsAsync()
        {
            return (await GetQueryableAsync())
                .Include(b => b.PricingPeriods);
        }

        public async Task<Booth?> FindByNumberAsync(string number, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Booths
                .AsNoTracking()
                .Where(b => b.Number == number.ToUpper())
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<Booth>> GetAvailableBoothsAsync(CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Booths
                .AsNoTracking()
                .Where(b => b.Status == BoothStatus.Available)
                .OrderBy(b => b.Number)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsNumberUniqueAsync(string number, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var query = dbContext.Booths
                .AsNoTracking()
                .Where(b => b.Number == number.ToUpper());

            if (excludeId.HasValue)
            {
                query = query.Where(b => b.Id != excludeId.Value);
            }

            return !await query.AnyAsync(cancellationToken);
        }

        public async Task<List<Booth>> GetListWithActiveRentalsAsync(
            int skipCount,
            int maxResultCount,
            string? filter = null,
            BoothStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();

            var query = dbContext.Booths
                .Include(b => b.PricingPeriods)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(b => b.Number.Contains(filter.ToUpper()));
            }

            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }

            return await query
                .OrderBy(b => b.Number)
                .Skip(skipCount)
                .Take(maxResultCount)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetCountAsync(string? filter = null, BoothStatus? status = null, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var query = dbContext.Booths
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(b => b.Number.Contains(filter.ToUpper()));
            }

            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }

            return await query.CountAsync(cancellationToken);
        }
    }
}