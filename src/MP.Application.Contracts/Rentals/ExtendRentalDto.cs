using System;
using System.ComponentModel.DataAnnotations;

namespace MP.Rentals
{
    public class ExtendRentalDto
    {
        [Required]
        public Guid RentalId { get; set; }

        [Required]
        [Display(Name = "Nowa data zakończenia")]
        public DateTime NewEndDate { get; set; }

        [Required]
        public ExtensionPaymentType PaymentType { get; set; }

        public string? TerminalTransactionId { get; set; }

        public string? TerminalReceiptNumber { get; set; }

        public int? OnlineTimeoutMinutes { get; set; }
    }
}