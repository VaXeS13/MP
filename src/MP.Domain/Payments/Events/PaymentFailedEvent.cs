using System;
using System.Collections.Generic;

namespace MP.Domain.Payments.Events
{
    /// <summary>
    /// Event published when a payment fails or times out
    /// </summary>
    public class PaymentFailedEvent
    {
        public Guid UserId { get; set; }
        public Guid? TenantId { get; set; }
        public string TransactionId { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "PLN";
        public string Reason { get; set; } = null!;
        public List<Guid> RentalIds { get; set; } = new();
        public DateTime FailedAt { get; set; } = DateTime.UtcNow;
    }
}
