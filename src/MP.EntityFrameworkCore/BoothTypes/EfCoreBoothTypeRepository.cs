using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MP.Domain.BoothTypes;
using MP.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace MP.EntityFrameworkCore.BoothTypes
{
    public class EfCoreBoothTypeRepository : EfCoreRepository<MPDbContext, BoothType, Guid>, IBoothTypeRepository
    {
        public EfCoreBoothTypeRepository(IDbContextProvider<MPDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public async Task<List<BoothType>> GetActiveTypesAsync(CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<BoothType?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
        }

        public async Task<bool> IsNameUniqueAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            var query = dbSet
                .AsNoTracking()
                .Where(x => x.Name == name);

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return !await query.AnyAsync(cancellationToken);
        }
    }
}