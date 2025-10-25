using System.ComponentModel.DataAnnotations;

namespace MP.OrganizationalUnits.Dtos
{
    public class JoinUnitDto
    {
        [Required(ErrorMessage = "Code is required")]
        [StringLength(50, ErrorMessage = "Code cannot exceed 50 characters")]
        public string Code { get; set; } = null!;
    }
}
