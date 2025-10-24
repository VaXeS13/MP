using System;

namespace MP.OrganizationalUnits.Dtos
{
    public class JoinUnitResultDto
    {
        public Guid UnitId { get; set; }
        public string UnitName { get; set; } = null!;
        public bool RequiresRegistration { get; set; }
    }
}
