using System;
using System.Collections.Generic;

namespace MP.Domain.Payments.Events
{
    /// <summary>
    /// Event published when a payment is successfully completed and verified
    /// </summary>
    public class PaymentCompletedEvent
    {
        public Guid UserId { get; set; }
        public string TransactionId { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "PLN";
        public List<Guid> RentalIds { get; set; } = new();
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public string PaymentMethod { get; set; } = "Przelewy24";
    }
}
