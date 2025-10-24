using System;

namespace MP.OrganizationalUnits.Dtos
{
    public class CreateRegistrationCodeDto
    {
        public Guid? RoleId { get; set; }
        public int? MaxUsageCount { get; set; }
        public int? ExpirationDays { get; set; }
    }
}
