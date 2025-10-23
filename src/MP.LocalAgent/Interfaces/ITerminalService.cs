using System.Threading;
using System.Threading.Tasks;
using MP.LocalAgent.Contracts.Commands;
using MP.LocalAgent.Contracts.Responses;
using MP.LocalAgent.Configuration;

namespace MP.LocalAgent.Interfaces
{
    /// <summary>
    /// Service for handling payment terminal operations
    /// </summary>
    public interface ITerminalService
    {
        /// <summary>
        /// Authorize a payment on the terminal
        /// </summary>
        Task<TerminalPaymentResponse> AuthorizePaymentAsync(AuthorizeTerminalPaymentCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Capture a previously authorized payment
        /// </summary>
        Task<TerminalPaymentResponse> CapturePaymentAsync(CaptureTerminalPaymentCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refund a payment
        /// </summary>
        Task<TerminalPaymentResponse> RefundPaymentAsync(RefundTerminalPaymentCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancel/void a payment
        /// </summary>
        Task<TerminalPaymentResponse> CancelPaymentAsync(CancelTerminalPaymentCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check terminal status
        /// </summary>
        Task<TerminalStatusResponse> CheckStatusAsync(CheckTerminalStatusCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Initialize the terminal service with configuration
        /// </summary>
        Task<bool> InitializeAsync(DeviceConfiguration config);

        /// <summary>
        /// Check if the terminal is ready for operations
        /// </summary>
        Task<bool> IsReadyAsync();

        /// <summary>
        /// Get terminal information
        /// </summary>
        Task<TerminalInfo> GetTerminalInfoAsync();

        /// <summary>
        /// Perform health check on the terminal
        /// </summary>
        Task<bool> PerformHealthCheckAsync();

        /// <summary>
        /// Event fired when terminal status changes
        /// </summary>
        event EventHandler<TerminalStatusEventArgs>? StatusChanged;

        /// <summary>
        /// Event fired when a payment is processed
        /// </summary>
        event EventHandler<PaymentProcessedEventArgs>? PaymentProcessed;
    }

    /// <summary>
    /// Terminal information
    /// </summary>
    public class TerminalInfo
    {
        public string Model { get; set; } = null!;
        public string? SerialNumber { get; set; }
        public string? FirmwareVersion { get; set; }
        public string ProviderId { get; set; } = null!;
        public string ConnectionType { get; set; } = null!;
        public bool IsInitialized { get; set; }
        public bool IsOnline { get; set; }
        public DateTime LastActivity { get; set; }
        public string? SupportedPaymentMethods { get; set; }
    }

    /// <summary>
    /// Terminal status event arguments
    /// </summary>
    public class TerminalStatusEventArgs : System.EventArgs
    {
        public Enums.DeviceStatus PreviousStatus { get; set; }
        public Enums.DeviceStatus CurrentStatus { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; } = System.DateTime.UtcNow;
    }

    /// <summary>
    /// Payment processed event arguments
    /// </summary>
    public class PaymentProcessedEventArgs : System.EventArgs
    {
        public string TransactionId { get; set; } = null!;
        public decimal Amount { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime ProcessedAt { get; set; } = System.DateTime.UtcNow;
        public string? CardType { get; set; }
        public string? LastFourDigits { get; set; }
    }
}