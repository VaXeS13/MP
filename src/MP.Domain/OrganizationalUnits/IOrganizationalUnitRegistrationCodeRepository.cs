using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.OrganizationalUnits
{
    /// <summary>
    /// Repository interface for OrganizationalUnitRegistrationCode
    /// Manages registration codes for organizational unit self-service signup
    /// </summary>
    public interface IOrganizationalUnitRegistrationCodeRepository : IRepository<OrganizationalUnitRegistrationCode, Guid>
    {
        /// <summary>
        /// Find registration code by code value
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="code">The code value</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The registration code or null if not found</returns>
        Task<OrganizationalUnitRegistrationCode?> FindByCodeAsync(
            Guid? tenantId,
            string code,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get registration code by code value, throwing exception if not found
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="code">The code value</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The registration code</returns>
        /// <exception cref="ArgumentException">Thrown when code not found</exception>
        Task<OrganizationalUnitRegistrationCode> GetByCodeAsync(
            Guid? tenantId,
            string code,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if code is unique within a tenant
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="code">The code to check</param>
        /// <param name="excludeId">Exclude specific code from uniqueness check (for updates)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if code is unique, false otherwise</returns>
        Task<bool> IsCodeUniqueAsync(
            Guid? tenantId,
            string code,
            Guid? excludeId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get active registration codes for organizational unit
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="organizationalUnitId">The organizational unit identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of active registration codes</returns>
        Task<List<OrganizationalUnitRegistrationCode>> GetActiveCodesForUnitAsync(
            Guid? tenantId,
            Guid organizationalUnitId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get registration codes with filtering and pagination
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="organizationalUnitId">Optional filter by organizational unit</param>
        /// <param name="isActive">Filter by active status (null = all)</param>
        /// <param name="skipCount">Number of items to skip</param>
        /// <param name="maxResultCount">Maximum items to return</param>
        /// <param name="sorting">Sort order</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of registration codes</returns>
        Task<List<OrganizationalUnitRegistrationCode>> GetListAsync(
            Guid? tenantId,
            Guid? organizationalUnitId = null,
            bool? isActive = null,
            int skipCount = 0,
            int maxResultCount = int.MaxValue,
            string sorting = "CreationTime DESC",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get total count of registration codes with filtering
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="organizationalUnitId">Optional filter by organizational unit</param>
        /// <param name="isActive">Filter by active status (null = all)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total count</returns>
        Task<long> GetCountAsync(
            Guid? tenantId,
            Guid? organizationalUnitId = null,
            bool? isActive = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Mark code as used by user
        /// </summary>
        /// <param name="codeId">The registration code identifier</param>
        /// <param name="userId">The user identifier who used the code</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task MarkCodeAsUsedAsync(
            Guid codeId,
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}
