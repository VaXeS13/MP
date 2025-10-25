using System;

namespace MP.OrganizationalUnits.Dtos
{
    public class GenerateRegistrationCodeRequestDto
    {
        public Guid OrganizationalUnitId { get; set; }
        public CreateRegistrationCodeDto CreateDto { get; set; } = null!;
    }
}
