using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MP.Items
{
    public class CreateBulkItemsDto
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        [MaxLength(50, ErrorMessage = "Maximum 50 items can be created at once")]
        public List<BulkItemEntryDto> Items { get; set; } = new();
    }

    public class BulkItemEntryDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(100)]
        public string? Category { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }
    }
}
