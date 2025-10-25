using System;
using System.ComponentModel.DataAnnotations;

namespace MP.OrganizationalUnits.Dtos
{
    public class CreateUpdateOrganizationalUnitDto
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Code is required")]
        [StringLength(50, ErrorMessage = "Code cannot exceed 50 characters")]
        [RegularExpression(@"^[a-zA-Z0-9-]+$", ErrorMessage = "Code can contain only alphanumeric characters and hyphens")]
        public string Code { get; set; } = null!;

        [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters")]
        public string? Address { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string? City { get; set; }

        [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters")]
        public string? PostalCode { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string? Email { get; set; }

        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
        public string? Phone { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
