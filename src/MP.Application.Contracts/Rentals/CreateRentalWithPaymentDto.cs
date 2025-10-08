using System;
using System.ComponentModel.DataAnnotations;

namespace MP.Rentals
{
    public class CreateRentalWithPaymentDto
    {
        [Required]
        [Display(Name = "Stanowisko")]
        public Guid BoothId { get; set; }

        [Required]
        [Display(Name = "Typ stanowiska")]
        public Guid BoothTypeId { get; set; }

        [Required]
        [Display(Name = "Data rozpoczęcia")]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "Data zakończenia")]
        public DateTime EndDate { get; set; }

        [StringLength(1000)]
        [Display(Name = "Notatki")]
        public string? Notes { get; set; }

        [Required]
        [Display(Name = "Dostawca płatności")]
        public string PaymentProviderId { get; set; } = null!;

        [Display(Name = "Metoda płatności")]
        public string? PaymentMethodId { get; set; }
    }
}