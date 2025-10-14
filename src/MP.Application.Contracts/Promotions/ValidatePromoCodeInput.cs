using System.ComponentModel.DataAnnotations;

namespace MP.Promotions
{
    public class ValidatePromoCodeInput
    {
        [Required]
        [StringLength(50)]
        public string PromoCode { get; set; } = null!;
    }
}
