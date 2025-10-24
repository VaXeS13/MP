using System;

namespace MP.Domain.OrganizationalUnits
{
    /// <summary>
    /// Provides access to the current organizational unit context
    /// Similar to ICurrentTenant but for organizational units
    /// </summary>
    public interface ICurrentOrganizationalUnit
    {
        /// <summary>
        /// Gets the ID of the current organizational unit
        /// </summary>
        /// <exception cref="CurrentOrganizationalUnitNotSetException">Thrown when no organizational unit is set and IsAvailable is false</exception>
        Guid? Id { get; }

        /// <summary>
        /// Gets the name of the current organizational unit
        /// </summary>
        /// <exception cref="CurrentOrganizationalUnitNotSetException">Thrown when no organizational unit is set and IsAvailable is false</exception>
        string? Name { get; }

        /// <summary>
        /// Indicates whether an organizational unit is currently set and available
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Temporarily changes the current organizational unit context
        /// Returns an IDisposable that restores the previous organizational unit when disposed
        /// </summary>
        /// <param name="id">The ID of the organizational unit to change to (null to clear)</param>
        /// <param name="name">Optional name of the organizational unit</param>
        /// <returns>An IDisposable that restores the previous organizational unit on disposal</returns>
        /// <example>
        /// using (_currentUnit.Change(newUnitId))
        /// {
        ///     // Code executes with the new organizational unit context
        ///     // This is useful for scoped operations
        /// }
        /// // Previous organizational unit is restored here
        /// </example>
        IDisposable Change(Guid? id, string? name = null);
    }
}
