using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Identity;

namespace MP.Domain.OrganizationalUnits
{
    public class UserOrganizationalUnit : Entity<Guid>
    {
        public Guid UserId { get; private set; }
        public Guid OrganizationalUnitId { get; private set; }
        public Guid? RoleId { get; private set; }
        public Guid? TenantId { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime AssignedAt { get; private set; }

        // Navigation properties
        public virtual IdentityUser? AppUser { get; set; }
        public virtual OrganizationalUnit? OrganizationalUnit { get; set; }

        // EF Core constructor
        private UserOrganizationalUnit() { }

        public UserOrganizationalUnit(
            Guid id,
            Guid userId,
            Guid organizationalUnitId,
            Guid? roleId = null,
            Guid? tenantId = null
        ) : base(id)
        {
            UserId = userId;
            OrganizationalUnitId = organizationalUnitId;
            RoleId = roleId;
            TenantId = tenantId;
            IsActive = true;
            AssignedAt = DateTime.UtcNow;
        }

        public void UpdateRole(Guid? roleId)
        {
            RoleId = roleId;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Activate()
        {
            IsActive = true;
        }
    }
}
