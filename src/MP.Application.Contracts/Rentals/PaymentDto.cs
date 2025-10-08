using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP.Rentals
{
    public class PaymentDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Kwota musi być większa od 0")]
        [Display(Name = "Kwota płatności")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Data płatności")]
        public DateTime PaidDate { get; set; } = DateTime.Now;
    }
}