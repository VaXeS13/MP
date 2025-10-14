using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace MP.Carts
{
    public class CartDto : EntityDto<Guid>
    {
        public Guid UserId { get; set; }
        public CartStatus Status { get; set; }
        public string StatusDisplayName { get; set; } = string.Empty;

        public List<CartItemDto> Items { get; set; } = new();

        // Calculated summary fields
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalDays { get; set; }

        // User info (optional)
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }

        // Promotion fields
        public Guid? AppliedPromotionId { get; set; }
        public string? PromotionName { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? PromoCodeUsed { get; set; }

        public DateTime CreationTime { get; set; }
        public DateTime? LastModificationTime { get; set; }
    }
}