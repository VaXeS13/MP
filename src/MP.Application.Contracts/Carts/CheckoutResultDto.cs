using System;
using System.Collections.Generic;

namespace MP.Carts
{
    public class CheckoutResultDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        // Payment info
        public string? TransactionId { get; set; }
        public string? PaymentUrl { get; set; }

        // Created rental IDs
        public List<Guid> RentalIds { get; set; } = new();

        // Summary
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
    }
}