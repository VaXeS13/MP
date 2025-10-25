using System;

namespace MP.OrganizationalUnits.Dtos
{
    public class OrganizationalUnitDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime? LastModificationTime { get; set; }

        public OrganizationalUnitSettingsDto? Settings { get; set; }
    }
}
