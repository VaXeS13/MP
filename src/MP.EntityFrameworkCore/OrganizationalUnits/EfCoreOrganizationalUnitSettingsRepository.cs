using Microsoft.EntityFrameworkCore;
using MP.Domain.OrganizationalUnits;
using MP.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace MP.OrganizationalUnits
{
    /// <summary>
    /// EF Core implementation of IOrganizationalUnitSettingsRepository
    /// </summary>
    public class EfCoreOrganizationalUnitSettingsRepository : EfCoreRepository<MPDbContext, OrganizationalUnitSettings, Guid>, IOrganizationalUnitSettingsRepository
    {
        public EfCoreOrganizationalUnitSettingsRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<OrganizationalUnitSettings?> GetByOrganizationalUnitAsync(
            Guid? tenantId,
            Guid organizationalUnitId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.OrganizationalUnitSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.OrganizationalUnitId == organizationalUnitId,
                    cancellationToken);
        }

        public async Task<bool> ExistsAsync(
            Guid? tenantId,
            Guid organizationalUnitId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.OrganizationalUnitSettings
                .AsNoTracking()
                .AnyAsync(x => x.TenantId == tenantId && x.OrganizationalUnitId == organizationalUnitId,
                    cancellationToken);
        }

        public async Task<OrganizationalUnitSettings> GetOrCreateAsync(
            Guid? tenantId,
            Guid organizationalUnitId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var existing = await dbContext.OrganizationalUnitSettings
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.OrganizationalUnitId == organizationalUnitId,
                    cancellationToken);

            if (existing != null)
            {
                return existing;
            }

            // Create new settings with defaults
            var newSettings = new OrganizationalUnitSettings(
                id: Guid.NewGuid(),
                organizationalUnitId: organizationalUnitId,
                tenantId: tenantId);

            await InsertAsync(newSettings, cancellationToken: cancellationToken);
            return newSettings;
        }

        public async Task DeleteByOrganizationalUnitAsync(
            Guid? tenantId,
            Guid organizationalUnitId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var settings = await dbContext.OrganizationalUnitSettings
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.OrganizationalUnitId == organizationalUnitId,
                    cancellationToken);

            if (settings != null)
            {
                await DeleteAsync(settings, cancellationToken: cancellationToken);
            }
        }
    }
}
