using System;

namespace MP.OrganizationalUnits.Dtos
{
    public class ValidateCodeResultDto
    {
        public bool IsValid { get; set; }
        public Guid? UnitId { get; set; }
        public string? UnitName { get; set; }
        public string? Reason { get; set; }
    }
}
