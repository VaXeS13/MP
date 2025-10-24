using System;

namespace MP.OrganizationalUnits.Dtos
{
    public class AssignUserDto
    {
        public Guid UserId { get; set; }
        public Guid? RoleId { get; set; }
    }
}
