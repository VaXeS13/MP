using System;
using System.Collections.Generic;
using MP.Rentals;

namespace MP.Payments
{
    public class PaymentSuccessViewModel
    {
        public PaymentTransactionDto Transaction { get; set; } = null!;
        public List<RentalDto> Rentals { get; set; } = new();
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public string NextStepUrl { get; set; } = null!;
        public string NextStepText { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = null!;
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public string FormattedPaymentDate { get; set; } = null!;
        public string FormattedTotalAmount { get; set; } = null!;

        // Dodatkowe informacje o płatności
        public string OrderId { get; set; } = null!;
        public string PaymentProvider { get; set; } = "Przelewy24";
        public bool IsVerified { get; set; }
        public string? Method { get; set; }
    }

    public class RentalSummaryDto
    {
        public Guid Id { get; set; }
        public string BoothNumber { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DaysCount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = null!;
        public RentalStatus Status { get; set; }
        public string StatusDisplayName { get; set; } = null!;
        public string FormattedAmount { get; set; } = null!;
        public string FormattedDateRange { get; set; } = null!;
    }
}