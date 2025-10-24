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
    /// EF Core implementation of IOrganizationalUnitRepository
    /// </summary>
    public class EfCoreOrganizationalUnitRepository : EfCoreRepository<MPDbContext, OrganizationalUnit, Guid>, IOrganizationalUnitRepository
    {
        public EfCoreOrganizationalUnitRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<OrganizationalUnit?> FindByCodeAsync(
            Guid? tenantId,
            string code,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.OrganizationalUnits
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Code == code.ToUpper())
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<OrganizationalUnit> GetByCodeAsync(
            Guid? tenantId,
            string code,
            CancellationToken cancellationToken = default)
        {
            var unit = await FindByCodeAsync(tenantId, code, cancellationToken);
            if (unit == null)
            {
                throw new ArgumentException($"Organizational unit with code '{code}' not found for tenant '{tenantId}'", nameof(code));
            }
            return unit;
        }

        public async Task<bool> IsCodeUniqueAsync(
            Guid? tenantId,
            string code,
            Guid? excludeId = null,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var query = dbContext.OrganizationalUnits
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Code == code.ToUpper());

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return !await query.AnyAsync(cancellationToken);
        }

        public async Task<List<OrganizationalUnit>> GetListAsync(
            Guid? tenantId,
            string? filterText = null,
            bool? isActive = null,
            int skipCount = 0,
            int maxResultCount = int.MaxValue,
            string sorting = "Name ASC",
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();

            var query = dbContext.OrganizationalUnits
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filterText))
            {
                var filter = filterText.ToUpper();
                query = query.Where(x => x.Code.Contains(filter) || x.Name.Contains(filter));
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
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
            string? filterText = null,
            bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var query = dbContext.OrganizationalUnits
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filterText))
            {
                var filter = filterText.ToUpper();
                query = query.Where(x => x.Code.Contains(filter) || x.Name.Contains(filter));
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            return await query.CountAsync(cancellationToken);
        }

        public async Task<List<OrganizationalUnit>> GetUserOrganizationalUnitsAsync(
            Guid? tenantId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.OrganizationalUnits
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        private IQueryable<OrganizationalUnit> ApplySorting(IQueryable<OrganizationalUnit> query, string sorting)
        {
            if (string.IsNullOrEmpty(sorting))
            {
                return query.OrderBy(x => x.Name);
            }

            var sortParts = sorting.Split(' ');
            var sortProperty = sortParts[0];
            var sortDirection = sortParts.Length > 1 && sortParts[1].Equals("DESC", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";

            return sortProperty.ToUpper() switch
            {
                "CODE" => sortDirection == "DESC" ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
                "ISACTIVE" => sortDirection == "DESC" ? query.OrderByDescending(x => x.IsActive) : query.OrderBy(x => x.IsActive),
                _ => sortDirection == "DESC" ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            };
        }
    }
}
