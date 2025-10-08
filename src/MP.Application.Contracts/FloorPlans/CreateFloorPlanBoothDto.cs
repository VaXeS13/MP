using System;
using System.ComponentModel.DataAnnotations;

namespace MP.FloorPlans
{
    public class CreateFloorPlanBoothDto
    {
        [Required]
        public Guid BoothId { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        [Display(Name = "Pozycja X")]
        public int X { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        [Display(Name = "Pozycja Y")]
        public int Y { get; set; }

        [Required]
        [Range(1, 1000)]
        [Display(Name = "Szerokość")]
        public int Width { get; set; }

        [Required]
        [Range(1, 1000)]
        [Display(Name = "Wysokość")]
        public int Height { get; set; }

        [Range(0, 359)]
        [Display(Name = "Obrót")]
        public int Rotation { get; set; } = 0;
    }
}