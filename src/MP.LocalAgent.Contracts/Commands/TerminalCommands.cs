using System;

namespace MP.LocalAgent.Contracts.Commands
{
    /// <summary>
    /// Command to authorize payment on terminal
    /// </summary>
    public class AuthorizeTerminalPaymentCommand
    {
        public Guid CommandId { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string TerminalProviderId { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "PLN";
        public string? Description { get; set; }
        public string? ReferenceId { get; set; }
        public Guid RentalItemId { get; set; }
        public string? RentalItemName { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public Dictionary<string, string>? AdditionalData { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);
    }

    /// <summary>
    /// Command to capture a previously authorized payment
    /// </summary>
    public class CaptureTerminalPaymentCommand
    {
        public Guid CommandId { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string TerminalProviderId { get; set; } = null!;
        public string TransactionId { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Command to refund a payment
    /// </summary>
    public class RefundTerminalPaymentCommand
    {
        public Guid CommandId { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string TerminalProviderId { get; set; } = null!;
        public string TransactionId { get; set; } = null!;
        public decimal Amount { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);
    }

    /// <summary>
    /// Command to cancel/void a payment
    /// </summary>
    public class CancelTerminalPaymentCommand
    {
        public Guid CommandId { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string TerminalProviderId { get; set; } = null!;
        public string TransactionId { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Command to check terminal status
    /// </summary>
    public class CheckTerminalStatusCommand
    {
        public Guid CommandId { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string TerminalProviderId { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}