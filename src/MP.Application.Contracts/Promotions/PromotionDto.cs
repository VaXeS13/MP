using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace MP.Promotions
{
    public class PromotionDto : FullAuditedEntityDto<Guid>
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public PromotionType Type { get; set; }
        public PromotionDisplayMode DisplayMode { get; set; }
        public bool IsActive { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public int Priority { get; set; }

        public int? MinimumBoothsCount { get; set; }
        public string? PromoCode { get; set; }
        public bool RequiresPromoCode { get; set; }

        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }

        public int? MaxUsageCount { get; set; }
        public int CurrentUsageCount { get; set; }
        public int? MaxUsagePerUser { get; set; }

        public string? CustomerMessage { get; set; }

        public List<Guid> ApplicableBoothTypeIds { get; set; } = new();
    }
}
