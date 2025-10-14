using System;
using System.ComponentModel.DataAnnotations;

namespace MP.Promotions
{
    public class CalculateDiscountInput
    {
        [Required]
        public Guid PromotionId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal TotalAmount { get; set; }
    }
}
