using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.OrganizationalUnits
{
    /// <summary>
    /// Repository interface for OrganizationalUnitSettings
    /// Manages organizational unit configuration and settings
    /// </summary>
    public interface IOrganizationalUnitSettingsRepository : IRepository<OrganizationalUnitSettings, Guid>
    {
        /// <summary>
        /// Get settings for specific organizational unit
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="organizationalUnitId">The organizational unit identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The organizational unit settings or null if not found</returns>
        Task<OrganizationalUnitSettings?> GetByOrganizationalUnitAsync(
            Guid? tenantId,
            Guid organizationalUnitId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if settings exist for organizational unit
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="organizationalUnitId">The organizational unit identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if settings exist, false otherwise</returns>
        Task<bool> ExistsAsync(
            Guid? tenantId,
            Guid organizationalUnitId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get or create settings for organizational unit
        /// Creates default settings if they don't exist
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="organizationalUnitId">The organizational unit identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The organizational unit settings (new or existing)</returns>
        Task<OrganizationalUnitSettings> GetOrCreateAsync(
            Guid? tenantId,
            Guid organizationalUnitId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete settings for organizational unit
        /// </summary>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="organizationalUnitId">The organizational unit identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task DeleteByOrganizationalUnitAsync(
            Guid? tenantId,
            Guid organizationalUnitId,
            CancellationToken cancellationToken = default);
    }
}
