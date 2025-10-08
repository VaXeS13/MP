using System.ComponentModel.DataAnnotations;
using MP.Domain.FloorPlans;

namespace MP.FloorPlans
{
    public class UpdateFloorPlanElementDto
    {
        [Required]
        public FloorPlanElementType ElementType { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int X { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int Y { get; set; }

        [Required]
        [Range(1, 5000)]
        public int Width { get; set; }

        [Required]
        [Range(1, 5000)]
        public int Height { get; set; }

        [Range(0, 359)]
        public int Rotation { get; set; }

        [MaxLength(20)]
        public string? Color { get; set; }

        [MaxLength(500)]
        public string? Text { get; set; }

        [MaxLength(50)]
        public string? IconName { get; set; }

        [Range(1, 100)]
        public int? Thickness { get; set; }

        [Range(0.0, 1.0)]
        public double? Opacity { get; set; }

        [MaxLength(20)]
        public string? Direction { get; set; }
    }
}
