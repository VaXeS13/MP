using System;

namespace MP.OrganizationalUnits.Dtos
{
    public class SwitchUnitDto
    {
        public Guid UnitId { get; set; }
        public string UnitName { get; set; } = null!;
        public bool CookieSet { get; set; }
    }
}
