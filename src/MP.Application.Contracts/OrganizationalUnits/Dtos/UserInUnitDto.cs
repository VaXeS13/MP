using System;

namespace MP.OrganizationalUnits.Dtos
{
    public class UserInUnitDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Role { get; set; }
        public DateTime AssignedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
