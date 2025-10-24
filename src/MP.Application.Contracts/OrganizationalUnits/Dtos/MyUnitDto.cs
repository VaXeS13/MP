using System;

namespace MP.OrganizationalUnits.Dtos
{
    public class MyUnitDto
    {
        public Guid UnitId { get; set; }
        public string UnitName { get; set; } = null!;
        public string UnitCode { get; set; } = null!;
        public string? Role { get; set; }
        public string? Currency { get; set; }
    }
}
