using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MP.Domain.HomePageContent;
using MP.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace MP.EntityFrameworkCore.HomePageContent
{
    public class HomePageSectionRepository :
        EfCoreRepository<MPDbContext, Domain.HomePageContent.HomePageSection, Guid>,
        IHomePageSectionRepository
    {
        public HomePageSectionRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<List<Domain.HomePageContent.HomePageSection>> GetActiveOrderedAsync(CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.HomePageSections
                .Where(s => s.IsActive)
                .OrderBy(s => s.Order)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Domain.HomePageContent.HomePageSection>> GetAllOrderedAsync(CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.HomePageSections
                .OrderBy(s => s.Order)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetMaxOrderAsync(CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();

            var maxOrder = await dbContext.HomePageSections
                .OrderByDescending(s => s.Order)
                .Select(s => (int?)s.Order)
                .FirstOrDefaultAsync(cancellationToken);

            return maxOrder ?? 0;
        }

        public async Task UpdateOrdersAsync(Dictionary<Guid, int> idOrderMap, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();

            var sections = await dbContext.HomePageSections
                .Where(s => idOrderMap.Keys.Contains(s.Id))
                .ToListAsync(cancellationToken);

            foreach (var section in sections)
            {
                if (idOrderMap.TryGetValue(section.Id, out var newOrder))
                {
                    section.SetOrder(newOrder);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
