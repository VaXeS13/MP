using System;

namespace MP.Rentals
{
    public class CreateRentalWithPaymentResultDto
    {
        public bool Success { get; set; }
        public Guid? RentalId { get; set; }
        public string? TransactionId { get; set; }
        public string? PaymentUrl { get; set; }
        public string? ErrorMessage { get; set; }
    }
}