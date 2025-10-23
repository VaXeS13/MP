using System.Threading;
using System.Threading.Tasks;
using MP.LocalAgent.Contracts.Commands;
using MP.LocalAgent.Contracts.Responses;
using MP.LocalAgent.Configuration;

namespace MP.LocalAgent.Interfaces
{
    /// <summary>
    /// Service for handling fiscal printer operations
    /// </summary>
    public interface IFiscalPrinterService
    {
        /// <summary>
        /// Print a fiscal receipt
        /// </summary>
        Task<FiscalReceiptResponse> PrintReceiptAsync(PrintFiscalReceiptCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Print a non-fiscal document
        /// </summary>
        Task<SimpleCommandResponse> PrintNonFiscalDocumentAsync(PrintNonFiscalDocumentCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get daily fiscal report (Z-report)
        /// </summary>
        Task<FiscalReportResponse> GetDailyReportAsync(GetDailyFiscalReportCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancel the last fiscal receipt
        /// </summary>
        Task<SimpleCommandResponse> CancelLastReceiptAsync(CancelLastFiscalReceiptCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check fiscal printer status
        /// </summary>
        Task<FiscalPrinterStatusResponse> CheckStatusAsync(CheckFiscalPrinterStatusCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Initialize the fiscal printer service with configuration
        /// </summary>
        Task<bool> InitializeAsync(DeviceConfiguration config);

        /// <summary>
        /// Check if the printer is ready for operations
        /// </summary>
        Task<bool> IsReadyAsync();

        /// <summary>
        /// Get fiscal printer information
        /// </summary>
        Task<FiscalPrinterInfo> GetPrinterInfoAsync();

        /// <summary>
        /// Perform health check on the fiscal printer
        /// </summary>
        Task<bool> PerformHealthCheckAsync();

        /// <summary>
        /// Get the last fiscal receipt number
        /// </summary>
        Task<string?> GetLastFiscalNumberAsync();

        /// <summary>
        /// Check if fiscal memory is healthy
        /// </summary>
        Task<bool> CheckFiscalMemoryHealthAsync();

        /// <summary>
        /// Event fired when printer status changes
        /// </summary>
        event EventHandler<FiscalPrinterStatusEventArgs>? StatusChanged;

        /// <summary>
        /// Event fired when a receipt is printed
        /// </summary>
        event EventHandler<ReceiptPrintedEventArgs>? ReceiptPrinted;

        /// <summary>
        /// Event fired when fiscal memory warning occurs
        /// </summary>
        event EventHandler<FiscalMemoryWarningEventArgs>? FiscalMemoryWarning;
    }

    /// <summary>
    /// Fiscal printer information
    /// </summary>
    public class FiscalPrinterInfo
    {
        public string Model { get; set; } = null!;
        public string? SerialNumber { get; set; }
        public string? FirmwareVersion { get; set; }
        public string ProviderId { get; set; } = null!;
        public string ConnectionType { get; set; } = null!;
        public bool IsInitialized { get; set; }
        public bool IsOnline { get; set; }
        public bool HasPaper { get; set; }
        public bool FiscalMemoryOk { get; set; }
        public int? FiscalMemoryUsagePercent { get; set; }
        public string? LastFiscalNumber { get; set; }
        public DateTime? LastReceiptDate { get; set; }
        public DateTime? LastZReportDate { get; set; }
        public string Region { get; set; } = "PL";
        public bool IsInFiscalMode { get; set; }
        public DateTime LastActivity { get; set; }
    }

    /// <summary>
    /// Fiscal printer status event arguments
    /// </summary>
    public class FiscalPrinterStatusEventArgs : System.EventArgs
    {
        public Enums.DeviceStatus PreviousStatus { get; set; }
        public Enums.DeviceStatus CurrentStatus { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; } = System.DateTime.UtcNow;
    }

    /// <summary>
    /// Receipt printed event arguments
    /// </summary>
    public class ReceiptPrintedEventArgs : System.EventArgs
    {
        public string FiscalNumber { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public DateTime PrintedAt { get; set; } = System.DateTime.UtcNow;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Fiscal memory warning event arguments
    /// </summary>
    public class FiscalMemoryWarningEventArgs : System.EventArgs
    {
        public int UsagePercent { get; set; }
        public string Message { get; set; } = null!;
        public DateTime Timestamp { get; set; } = System.DateTime.UtcNow;
        public bool IsCritical { get; set; }
    }
}