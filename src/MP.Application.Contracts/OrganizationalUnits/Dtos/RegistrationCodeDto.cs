using System;

namespace MP.OrganizationalUnits.Dtos
{
    public class RegistrationCodeDto
    {
        public Guid Id { get; set; }
        public Guid OrganizationalUnitId { get; set; }
        public string Code { get; set; } = null!;
        public Guid? RoleId { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int? MaxUsageCount { get; set; }
        public int UsageCount { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsExpired { get; set; }
        public bool IsUsageLimitReached { get; set; }
    }
}
