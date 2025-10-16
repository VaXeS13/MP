using System;
using System.Collections.Generic;

namespace MP.Domain.Payments.Events
{
    /// <summary>
    /// Event published when a payment process is initiated (e.g., after checkout)
    /// </summary>
    public class PaymentInitiatedEvent
    {
        public Guid UserId { get; set; }
        public string TransactionId { get; set; } = null!;
        public string SessionId { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public List<Guid> RentalIds { get; set; } = new();
        public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
    }
}
