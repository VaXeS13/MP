using System.ComponentModel.DataAnnotations;

namespace MP.Carts
{
    public class CheckoutCartDto
    {
        [Required]
        [Display(Name = "Payment Provider")]
        public string PaymentProviderId { get; set; } = null!;

        [Display(Name = "Payment Method")]
        public string? PaymentMethodId { get; set; }
    }
}