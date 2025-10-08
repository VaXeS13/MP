using System.ComponentModel.DataAnnotations;

namespace MP.Application.Contracts.Payments
{
    public class P24NotificationDto
    {
        [Required]
        public string P24_merchant_id { get; set; } = null!;

        [Required]
        public string P24_pos_id { get; set; } = null!;

        [Required]
        public string P24_session_id { get; set; } = null!;

        [Required]
        public int P24_amount { get; set; }

        [Required]
        public string P24_currency { get; set; } = null!;

        [Required]
        public string P24_order_id { get; set; } = null!;

        public int P24_method { get; set; }

        public string P24_statement { get; set; } = "";

        public string P24_sign { get; set; } = null!;
    }
}