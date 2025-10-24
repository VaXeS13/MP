using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.OrganizationalUnits
{
    /// <summary>
    /// Repository interface for UserOrganizationalUnit (many-to-many relationship)
    /// Manages user membership in organizational units
    /// </summary>
    public interface IUserOrganizationalUnitRepository : IRepository<UserOrganizationalUnit, Guid>
    {
        /// <summary>
        /// Check if user is member of organizational unit
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="userId">The user identifier</param>
        /// <param name="organizationalUnitId">The organizational unit identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if user is member, false otherwise</returns>
        Task<bool> IsMemberAsync(
            Guid? tenantId,
            Guid userId,
            Guid organizationalUnitId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get organizational units for a specific user
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="userId">The user identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of organizational unit identifiers user belongs to</returns>
        Task<List<Guid>> GetUserOrganizationalUnitIdsAsync(
            Guid? tenantId,
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get users in organizational unit
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="organizationalUnitId">The organizational unit identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of user identifiers in the organizational unit</returns>
        Task<List<Guid>> GetOrganizationalUnitUserIdsAsync(
            Guid? tenantId,
            Guid organizationalUnitId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get user organizational unit memberships with pagination
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="userId">The user identifier</param>
        /// <param name="skipCount">Number of items to skip</param>
        /// <param name="maxResultCount">Maximum items to return</param>
        /// <param name="sorting">Sort order</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of user organizational unit memberships</returns>
        Task<List<UserOrganizationalUnit>> GetUserMembershipsAsync(
            Guid? tenantId,
            Guid userId,
            int skipCount = 0,
            int maxResultCount = int.MaxValue,
            string sorting = "CreationTime DESC",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get total count of users in organizational unit
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="organizationalUnitId">The organizational unit identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total count of users</returns>
        Task<long> GetOrganizationalUnitMemberCountAsync(
            Guid? tenantId,
            Guid organizationalUnitId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove user from organizational unit
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="userId">The user identifier</param>
        /// <param name="organizationalUnitId">The organizational unit identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RemoveUserFromUnitAsync(
            Guid? tenantId,
            Guid userId,
            Guid organizationalUnitId,
            CancellationToken cancellationToken = default);
    }
}
