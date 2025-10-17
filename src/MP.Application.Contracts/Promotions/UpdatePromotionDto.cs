using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MP.Promotions
{
    public class UpdatePromotionDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public PromotionType Type { get; set; }

        [Required]
        public PromotionDisplayMode DisplayMode { get; set; }

        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }

        [Range(0, int.MaxValue)]
        public int Priority { get; set; }

        [Range(1, int.MaxValue)]
        public int? MinimumBoothsCount { get; set; }

        [StringLength(50)]
        public string? PromoCode { get; set; }

        [Required]
        public DiscountType DiscountType { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal DiscountValue { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? MaxDiscountAmount { get; set; }

        [Range(1, int.MaxValue)]
        public int? MaxUsageCount { get; set; }

        [Range(1, int.MaxValue)]
        public int? MaxUsagePerUser { get; set; }

        [StringLength(500)]
        public string? CustomerMessage { get; set; }

        [Range(1, int.MaxValue)]
        public int? MaxAccountAgeDays { get; set; }

        public bool IsActive { get; set; }

        public List<Guid> ApplicableBoothTypeIds { get; set; } = new();

        public List<Guid> ApplicableBoothIds { get; set; } = new();
    }
}
