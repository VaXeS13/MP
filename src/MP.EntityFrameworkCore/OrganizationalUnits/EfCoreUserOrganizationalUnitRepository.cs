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
    /// EF Core implementation of IUserOrganizationalUnitRepository
    /// </summary>
    public class EfCoreUserOrganizationalUnitRepository : EfCoreRepository<MPDbContext, UserOrganizationalUnit, Guid>, IUserOrganizationalUnitRepository
    {
        public EfCoreUserOrganizationalUnitRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<bool> IsMemberAsync(
            Guid? tenantId,
            Guid userId,
            Guid organizationalUnitId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.UserOrganizationalUnits
                .AsNoTracking()
                .AnyAsync(x => x.TenantId == tenantId &&
                              x.UserId == userId &&
                              x.OrganizationalUnitId == organizationalUnitId &&
                              x.IsActive,
                    cancellationToken);
        }

        public async Task<List<Guid>> GetUserOrganizationalUnitIdsAsync(
            Guid? tenantId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.UserOrganizationalUnits
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.UserId == userId && x.IsActive)
                .Select(x => x.OrganizationalUnitId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Guid>> GetOrganizationalUnitUserIdsAsync(
            Guid? tenantId,
            Guid organizationalUnitId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.UserOrganizationalUnits
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                           x.OrganizationalUnitId == organizationalUnitId &&
                           x.IsActive)
                .Select(x => x.UserId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<UserOrganizationalUnit>> GetUserMembershipsAsync(
            Guid? tenantId,
            Guid userId,
            int skipCount = 0,
            int maxResultCount = int.MaxValue,
            string sorting = "AssignedAt DESC",
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();

            var query = dbContext.UserOrganizationalUnits
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.UserId == userId)
                .AsQueryable();

            // Apply sorting
            query = ApplySorting(query, sorting);

            return await query
                .Skip(skipCount)
                .Take(maxResultCount)
                .ToListAsync(cancellationToken);
        }

        public async Task<long> GetOrganizationalUnitMemberCountAsync(
            Guid? tenantId,
            Guid organizationalUnitId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.UserOrganizationalUnits
                .AsNoTracking()
                .CountAsync(x => x.TenantId == tenantId &&
                               x.OrganizationalUnitId == organizationalUnitId &&
                               x.IsActive,
                    cancellationToken);
        }

        public async Task RemoveUserFromUnitAsync(
            Guid? tenantId,
            Guid userId,
            Guid organizationalUnitId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var membership = await dbContext.UserOrganizationalUnits
                .FirstOrDefaultAsync(x => x.TenantId == tenantId &&
                                         x.UserId == userId &&
                                         x.OrganizationalUnitId == organizationalUnitId,
                    cancellationToken);

            if (membership != null)
            {
                await DeleteAsync(membership, cancellationToken: cancellationToken);
            }
        }

        private IQueryable<UserOrganizationalUnit> ApplySorting(IQueryable<UserOrganizationalUnit> query, string sorting)
        {
            if (string.IsNullOrEmpty(sorting))
            {
                return query.OrderByDescending(x => x.AssignedAt);
            }

            var sortParts = sorting.Split(' ');
            var sortProperty = sortParts[0];
            var sortDirection = sortParts.Length > 1 && sortParts[1].Equals("DESC", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";

            return sortProperty.ToUpper() switch
            {
                "ASSIGNEDAT" => sortDirection == "DESC" ? query.OrderByDescending(x => x.AssignedAt) : query.OrderBy(x => x.AssignedAt),
                "ISACTIVE" => sortDirection == "DESC" ? query.OrderByDescending(x => x.IsActive) : query.OrderBy(x => x.IsActive),
                _ => sortDirection == "DESC" ? query.OrderByDescending(x => x.AssignedAt) : query.OrderBy(x => x.AssignedAt),
            };
        }
    }
}
