using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.OrganizationalUnits
{
    /// <summary>
    /// Represents a registration code for joining an organizational unit.
    /// Users can use this code to request access to a unit, with optional auto-assignment of role.
    /// Codes are human-readable in format: {TenantCode}-{UnitCode}-{RandomPart}
    /// Example: CTO-MAIN-ABC123, WARSAW-CENTER-XYZ789
    /// </summary>
    public class OrganizationalUnitRegistrationCode : FullAuditedEntity<Guid>, IMultiTenant
    {
        /// <summary>
        /// The tenant ID this code belongs to.
        /// </summary>
        public Guid? TenantId { get; private set; }

        /// <summary>
        /// The organizational unit this code is for.
        /// </summary>
        public Guid OrganizationalUnitId { get; private set; }

        /// <summary>
        /// The unique registration code in format: {TenantCode}-{UnitCode}-{Random}
        /// Example: CTO-MAIN-ABC123
        /// </summary>
        public string Code { get; private set; } = null!;

        /// <summary>
        /// Optional role ID to auto-assign when user uses this code.
        /// If null, user gets default role for new members.
        /// </summary>
        public Guid? RoleId { get; private set; }

        /// <summary>
        /// Optional expiration date for this code.
        /// If null, code never expires.
        /// </summary>
        public DateTime? ExpiresAt { get; private set; }

        /// <summary>
        /// Optional maximum number of times this code can be used.
        /// If null, code can be used unlimited times.
        /// </summary>
        public int? MaxUsageCount { get; private set; }

        /// <summary>
        /// Current number of times this code has been used.
        /// </summary>
        public int UsageCount { get; private set; }

        /// <summary>
        /// The date and time this code was last used.
        /// </summary>
        public DateTime? LastUsedAt { get; private set; }

        /// <summary>
        /// Whether this code is active (not deactivated by admin).
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Navigation property to the organizational unit.
        /// </summary>
        public virtual OrganizationalUnit OrganizationalUnit { get; set; } = null!;

        /// <summary>
        /// Private constructor for EF Core.
        /// </summary>
        private OrganizationalUnitRegistrationCode()
        {
        }

        /// <summary>
        /// Creates a new registration code for an organizational unit.
        /// </summary>
        /// <param name="id">Unique identifier for the code.</param>
        /// <param name="code">The registration code (format: {TENANT}-{UNIT}-{RANDOM}).</param>
        /// <param name="organizationalUnitId">The unit this code is for.</param>
        /// <param name="tenantId">Optional tenant ID.</param>
        /// <param name="roleId">Optional role to auto-assign.</param>
        /// <param name="maxUsageCount">Optional usage limit.</param>
        /// <param name="expirationDays">Optional expiration in days from now.</param>
        public OrganizationalUnitRegistrationCode(
            Guid id,
            string code,
            Guid organizationalUnitId,
            Guid? tenantId = null,
            Guid? roleId = null,
            int? maxUsageCount = null,
            int? expirationDays = null) : base(id)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new BusinessException("REGISTRATION_CODE_REQUIRED");

            if (organizationalUnitId == Guid.Empty)
                throw new ArgumentException("Organizational unit ID cannot be empty.", nameof(organizationalUnitId));

            if (maxUsageCount.HasValue && maxUsageCount <= 0)
                throw new BusinessException("REGISTRATION_CODE_MAX_USAGE_MUST_BE_POSITIVE");

            TenantId = tenantId;
            Code = code.Trim().ToUpper();
            OrganizationalUnitId = organizationalUnitId;
            RoleId = roleId;
            MaxUsageCount = maxUsageCount;
            UsageCount = 0;
            IsActive = true;

            if (expirationDays.HasValue && expirationDays > 0)
            {
                ExpiresAt = DateTime.UtcNow.AddDays(expirationDays.Value);
            }
        }

        /// <summary>
        /// Checks if this code can be used (active, not expired, usage limit not reached).
        /// </summary>
        /// <returns>True if code can be used, false otherwise.</returns>
        public bool CanBeUsed()
        {
            if (!IsActive)
                return false;

            if (IsExpired())
                return false;

            if (IsUsageLimitReached())
                return false;

            return true;
        }

        /// <summary>
        /// Increments the usage count and sets the last used timestamp.
        /// Should be called after a user successfully uses the code.
        /// </summary>
        public void IncrementUsageCount()
        {
            if (!CanBeUsed())
                throw new BusinessException("REGISTRATION_CODE_CANNOT_BE_USED");

            UsageCount++;
            LastUsedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if this code has expired.
        /// </summary>
        public bool IsExpired()
        {
            if (!ExpiresAt.HasValue)
                return false;

            return DateTime.UtcNow > ExpiresAt.Value;
        }

        /// <summary>
        /// Checks if the usage limit has been reached.
        /// </summary>
        public bool IsUsageLimitReached()
        {
            if (!MaxUsageCount.HasValue)
                return false;

            return UsageCount >= MaxUsageCount.Value;
        }

        /// <summary>
        /// Deactivates this code, preventing further use.
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
        }

        /// <summary>
        /// Reactivates a previously deactivated code.
        /// </summary>
        public void Reactivate()
        {
            IsActive = true;
        }
    }
}
