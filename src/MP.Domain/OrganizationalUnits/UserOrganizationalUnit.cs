using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.OrganizationalUnits
{
    /// <summary>
    /// Represents the assignment of a user to an organizational unit.
    /// This is a many-to-many relationship allowing users to belong to multiple units
    /// within a tenant, with potentially different roles in each unit.
    /// </summary>
    public class UserOrganizationalUnit : Entity<Guid>, IMultiTenant
    {
        /// <summary>
        /// The tenant ID this assignment belongs to.
        /// </summary>
        public Guid? TenantId { get; private set; }

        /// <summary>
        /// The user ID of the assigned user.
        /// </summary>
        public Guid UserId { get; private set; }

        /// <summary>
        /// The organizational unit ID being assigned.
        /// </summary>
        public Guid OrganizationalUnitId { get; private set; }

        /// <summary>
        /// Optional role ID specific to this user within this unit.
        /// If null, user inherits their tenant-level role.
        /// </summary>
        public Guid? RoleId { get; private set; }

        /// <summary>
        /// Whether this assignment is active.
        /// Inactive assignments are soft-deleted (user loses access but history is preserved).
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// The date and time when user was assigned to this unit.
        /// </summary>
        public DateTime AssignedAt { get; private set; }

        /// <summary>
        /// Navigation property to the assigned user.
        /// </summary>
        public virtual IdentityUser AppUser { get; set; } = null!;

        /// <summary>
        /// Navigation property to the organizational unit.
        /// </summary>
        public virtual OrganizationalUnit OrganizationalUnit { get; set; } = null!;

        /// <summary>
        /// Private constructor for EF Core.
        /// </summary>
        private UserOrganizationalUnit()
        {
        }

        /// <summary>
        /// Creates a new user-to-organizational-unit assignment.
        /// </summary>
        /// <param name="id">Unique identifier for the assignment.</param>
        /// <param name="userId">The user being assigned.</param>
        /// <param name="organizationalUnitId">The organizational unit to assign to.</param>
        /// <param name="roleId">Optional role specific to this unit assignment.</param>
        /// <param name="tenantId">Optional tenant ID.</param>
        public UserOrganizationalUnit(
            Guid id,
            Guid userId,
            Guid organizationalUnitId,
            Guid? roleId = null,
            Guid? tenantId = null) : base(id)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            if (organizationalUnitId == Guid.Empty)
                throw new ArgumentException("Organizational unit ID cannot be empty.", nameof(organizationalUnitId));

            TenantId = tenantId;
            UserId = userId;
            OrganizationalUnitId = organizationalUnitId;
            RoleId = roleId;
            IsActive = true;
            AssignedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the role for this user within this unit.
        /// </summary>
        /// <param name="newRoleId">New role ID, or null to inherit tenant-level role.</param>
        public void UpdateRole(Guid? newRoleId)
        {
            RoleId = newRoleId;
        }

        /// <summary>
        /// Deactivates this assignment, soft-removing the user from the unit.
        /// User loses access but assignment history is preserved.
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
        }

        /// <summary>
        /// Reactivates a previously deactivated assignment.
        /// </summary>
        public void Reactivate()
        {
            IsActive = true;
        }
    }
}
