using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace MP.FloorPlans
{
    public class CreateFloorPlanDto
    {
        [Required]
        public Guid OrganizationalUnitId { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1)]
        [Display(Name = "Nazwa planu")]
        public string Name { get; set; } = null!;

        [Required]
        [Range(0, 50)]
        [Display(Name = "Poziom piętra")]
        public int Level { get; set; }

        [Required]
        [Range(100, 10000)]
        [Display(Name = "Szerokość")]
        public int Width { get; set; }

        [Required]
        [Range(100, 10000)]
        [Display(Name = "Wysokość")]
        public int Height { get; set; }

        public List<CreateFloorPlanBoothDto> Booths { get; set; } = new();

        public List<CreateFloorPlanElementDto> Elements { get; set; } = new();
    }
}