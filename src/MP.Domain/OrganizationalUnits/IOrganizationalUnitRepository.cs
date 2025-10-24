using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.OrganizationalUnits
{
    /// <summary>
    /// Repository interface for OrganizationalUnit aggregate root
    /// Provides data access operations for organizational units
    /// </summary>
    public interface IOrganizationalUnitRepository : IRepository<OrganizationalUnit, Guid>
    {
        /// <summary>
        /// Find organizational unit by code within a tenant
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="code">The organizational unit code</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The organizational unit or null if not found</returns>
        Task<OrganizationalUnit?> FindByCodeAsync(
            Guid? tenantId,
            string code,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get organizational unit by code, throwing exception if not found
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="code">The organizational unit code</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The organizational unit</returns>
        /// <exception cref="ArgumentException">Thrown when unit not found</exception>
        Task<OrganizationalUnit> GetByCodeAsync(
            Guid? tenantId,
            string code,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if code is unique within a tenant
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="code">The code to check</param>
        /// <param name="excludeId">Exclude specific unit from uniqueness check (for updates)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if code is unique, false otherwise</returns>
        Task<bool> IsCodeUniqueAsync(
            Guid? tenantId,
            string code,
            Guid? excludeId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get list of organizational units for a tenant with pagination
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="filterText">Optional filter text (searches in code and name)</param>
        /// <param name="isActive">Filter by active status (null = all)</param>
        /// <param name="skipCount">Number of items to skip</param>
        /// <param name="maxResultCount">Maximum items to return</param>
        /// <param name="sorting">Sort order</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of organizational units</returns>
        Task<List<OrganizationalUnit>> GetListAsync(
            Guid? tenantId,
            string? filterText = null,
            bool? isActive = null,
            int skipCount = 0,
            int maxResultCount = int.MaxValue,
            string sorting = "Name ASC",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get total count of organizational units for a tenant with filtering
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="filterText">Optional filter text (searches in code and name)</param>
        /// <param name="isActive">Filter by active status (null = all)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total count</returns>
        Task<long> GetCountAsync(
            Guid? tenantId,
            string? filterText = null,
            bool? isActive = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get organizational units for a specific user
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="userId">The user identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of organizational units the user belongs to</returns>
        Task<List<OrganizationalUnit>> GetUserOrganizationalUnitsAsync(
            Guid? tenantId,
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}
