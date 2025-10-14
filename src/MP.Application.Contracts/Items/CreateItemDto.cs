using System.ComponentModel.DataAnnotations;

namespace MP.Items
{
    public class CreateItemDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(100)]
        public string? Category { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        // Note: Currency is automatically set from tenant settings
    }
}
