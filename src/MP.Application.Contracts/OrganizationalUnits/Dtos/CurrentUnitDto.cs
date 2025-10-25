using System;

namespace MP.OrganizationalUnits.Dtos
{
    public class CurrentUnitDto
    {
        public Guid UnitId { get; set; }
        public string UnitName { get; set; } = null!;
        public string UnitCode { get; set; } = null!;
        public string Currency { get; set; } = "PLN";
        public string? UserRole { get; set; }
        public OrganizationalUnitSettingsDto? Settings { get; set; }
    }
}
