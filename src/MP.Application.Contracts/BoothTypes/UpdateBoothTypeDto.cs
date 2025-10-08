using System.ComponentModel.DataAnnotations;

namespace MP.Application.Contracts.BoothTypes
{
    public class UpdateBoothTypeDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = null!;

        [Range(0, 100)]
        public decimal CommissionPercentage { get; set; }
    }
}