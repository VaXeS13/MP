using System.ComponentModel.DataAnnotations;

namespace MP.Promotions
{
    public class ApplyPromotionToCartInput
    {
        [StringLength(50)]
        public string? PromoCode { get; set; }
    }
}
