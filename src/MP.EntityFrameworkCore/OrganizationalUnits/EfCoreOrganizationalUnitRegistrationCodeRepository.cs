using Microsoft.EntityFrameworkCore;
using MP.Domain.OrganizationalUnits;
using MP.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace MP.OrganizationalUnits
{
    /// <summary>
    /// EF Core implementation of IOrganizationalUnitRegistrationCodeRepository
    /// </summary>
    public class EfCoreOrganizationalUnitRegistrationCodeRepository : EfCoreRepository<MPDbContext, OrganizationalUnitRegistrationCode, Guid>, IOrganizationalUnitRegistrationCodeRepository
    {
        public EfCoreOrganizationalUnitRegistrationCodeRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<OrganizationalUnitRegistrationCode?> FindByCodeAsync(
            Guid? tenantId,
            string code,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.OrganizationalUnitRegistrationCodes
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Code == code.ToUpper())
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<OrganizationalUnitRegistrationCode> GetByCodeAsync(
            Guid? tenantId,
            string code,
            CancellationToken cancellationToken = default)
        {
            var registrationCode = await FindByCodeAsync(tenantId, code, cancellationToken);
            if (registrationCode == null)
            {
                throw new ArgumentException($"Registration code '{code}' not found for tenant '{tenantId}'", nameof(code));
            }
            return registrationCode;
        }

        public async Task<bool> IsCodeUniqueAsync(
            Guid? tenantId,
            string code,
            Guid? excludeId = null,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var query = dbContext.OrganizationalUnitRegistrationCodes
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Code == code.ToUpper());

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return !await query.AnyAsync(cancellationToken);
        }

        public async Task<List<OrganizationalUnitRegistrationCode>> GetActiveCodesForUnitAsync(
            Guid? tenantId,
            Guid organizationalUnitId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var now = DateTime.UtcNow;

            return await dbContext.OrganizationalUnitRegistrationCodes
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                           x.OrganizationalUnitId == organizationalUnitId &&
                           x.ExpiresAt > now)
                .OrderByDescending(x => x.ExpiresAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<OrganizationalUnitRegistrationCode>> GetListAsync(
            Guid? tenantId,
            Guid? organizationalUnitId = null,
            bool? isActive = null,
            int skipCount = 0,
            int maxResultCount = int.MaxValue,
            string sorting = "CreationTime DESC",
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var now = DateTime.UtcNow;

            var query = dbContext.OrganizationalUnitRegistrationCodes
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .AsQueryable();

            if (organizationalUnitId.HasValue)
            {
                query = query.Where(x => x.OrganizationalUnitId == organizationalUnitId.Value);
            }

            if (isActive.HasValue)
            {
                if (isActive.Value)
                {
                    query = query.Where(x => x.ExpiresAt > now);
                }
                else
                {
                    query = query.Where(x => x.ExpiresAt <= now);
                }
            }

            // Apply sorting
            query = ApplySorting(query, sorting);

            return await query
                .Skip(skipCount)
                .Take(maxResultCount)
                .ToListAsync(cancellationToken);
        }

        public async Task<long> GetCountAsync(
            Guid? tenantId,
            Guid? organizationalUnitId = null,
            bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var now = DateTime.UtcNow;

            var query = dbContext.OrganizationalUnitRegistrationCodes
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .AsQueryable();

            if (organizationalUnitId.HasValue)
            {
                query = query.Where(x => x.OrganizationalUnitId == organizationalUnitId.Value);
            }

            if (isActive.HasValue)
            {
                if (isActive.Value)
                {
                    query = query.Where(x => x.ExpiresAt > now);
                }
                else
                {
                    query = query.Where(x => x.ExpiresAt <= now);
                }
            }

            return await query.CountAsync(cancellationToken);
        }

        public async Task MarkCodeAsUsedAsync(
            Guid codeId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var code = await GetAsync(codeId, cancellationToken: cancellationToken);

            // Increment usage count
            code.IncrementUsageCount();

            await UpdateAsync(code, cancellationToken: cancellationToken);
        }

        private IQueryable<OrganizationalUnitRegistrationCode> ApplySorting(IQueryable<OrganizationalUnitRegistrationCode> query, string sorting)
        {
            if (string.IsNullOrEmpty(sorting))
            {
                return query.OrderByDescending(x => x.CreationTime);
            }

            var sortParts = sorting.Split(' ');
            var sortProperty = sortParts[0];
            var sortDirection = sortParts.Length > 1 && sortParts[1].Equals("DESC", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";

            return sortProperty.ToUpper() switch
            {
                "EXPIRESAT" => sortDirection == "DESC" ? query.OrderByDescending(x => x.ExpiresAt) : query.OrderBy(x => x.ExpiresAt),
                "CODE" => sortDirection == "DESC" ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
                _ => sortDirection == "DESC" ? query.OrderByDescending(x => x.CreationTime) : query.OrderBy(x => x.CreationTime),
            };
        }
    }
}
