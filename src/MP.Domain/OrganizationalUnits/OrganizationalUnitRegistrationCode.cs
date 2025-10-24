using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace MP.Domain.OrganizationalUnits
{
    public class OrganizationalUnitRegistrationCode : FullAuditedEntity<Guid>
    {
        public Guid? TenantId { get; private set; }
        public Guid OrganizationalUnitId { get; private set; }
        public string Code { get; private set; } = null!;
        public Guid? RoleId { get; private set; }
        public DateTime? ExpiresAt { get; private set; }
        public int? MaxUsageCount { get; private set; }
        public int UsageCount { get; private set; }
        public DateTime? LastUsedAt { get; private set; }
        public bool IsActive { get; private set; }

        // Navigation properties
        public virtual OrganizationalUnit? OrganizationalUnit { get; set; }

        // EF Core constructor
        private OrganizationalUnitRegistrationCode() { }

        public OrganizationalUnitRegistrationCode(
            Guid id,
            Guid organizationalUnitId,
            string code,
            Guid? roleId = null,
            DateTime? expiresAt = null,
            int? maxUsageCount = null,
            Guid? tenantId = null
        ) : base(id)
        {
            TenantId = tenantId;
            OrganizationalUnitId = organizationalUnitId;
            SetCode(code);
            RoleId = roleId;
            ValidateExpiryDate(expiresAt);
            ExpiresAt = expiresAt;
            ValidateMaxUsageCount(maxUsageCount);
            MaxUsageCount = maxUsageCount;
            UsageCount = 0;
            LastUsedAt = null;
            IsActive = true;
        }

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

        public void IncrementUsageCount()
        {
            if (!CanBeUsed())
                throw new BusinessException("RegistrationCode.CannotUse", "This registration code cannot be used");

            UsageCount++;
            LastUsedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public bool IsExpired()
        {
            if (!ExpiresAt.HasValue)
                return false;

            return DateTime.UtcNow > ExpiresAt.Value;
        }

        public bool IsUsageLimitReached()
        {
            if (!MaxUsageCount.HasValue)
                return false;

            return UsageCount >= MaxUsageCount.Value;
        }

        public void SetRoleId(Guid roleId)
        {
            RoleId = roleId;
        }

        public void SetMaxUsageCount(int maxUsageCount)
        {
            ValidateMaxUsageCount(maxUsageCount);
            MaxUsageCount = maxUsageCount;
        }

        public void SetExpiresAt(DateTime expiresAt)
        {
            ValidateExpiryDate(expiresAt);
            ExpiresAt = expiresAt;
        }

        public void SetLastUsedAt(DateTime lastUsedAt)
        {
            LastUsedAt = lastUsedAt;
        }

        private void SetCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new BusinessException("RegistrationCode.CodeRequired", "Code is required");

            if (code.Length > 50)
                throw new BusinessException("RegistrationCode.CodeTooLong", "Code cannot exceed 50 characters");

            Code = code;
        }

        private void ValidateExpiryDate(DateTime? expiresAt)
        {
            if (expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow)
                throw new BusinessException("RegistrationCode.ExpiryDateInvalid", "Expiry date must be in the future");
        }

        private void ValidateMaxUsageCount(int? maxUsageCount)
        {
            if (maxUsageCount.HasValue && maxUsageCount.Value <= 0)
                throw new BusinessException("RegistrationCode.MaxUsageCountInvalid", "Max usage count must be greater than 0");
        }
    }
}
