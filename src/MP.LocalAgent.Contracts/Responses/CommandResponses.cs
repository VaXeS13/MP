using System;

namespace MP.LocalAgent.Contracts.Responses
{
    /// <summary>
    /// Base response for all commands
    /// </summary>
    public abstract class CommandResponseBase
    {
        public Guid CommandId { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan ProcessingDuration { get; set; }
        public Dictionary<string, object> ProviderData { get; set; } = new();
    }

    /// <summary>
    /// Response for terminal payment operations
    /// </summary>
    public class TerminalPaymentResponse : CommandResponseBase
    {
        public string? TransactionId { get; set; }
        public string? AuthorizationCode { get; set; }
        public string Status { get; set; } = null!; // "authorized", "captured", "declined", "error", "cancelled"
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public new DateTime? ProcessedAt { get; set; }
        public DateTime? Timestamp { get; set; }
        public string? CardType { get; set; }
        public string? LastFourDigits { get; set; }
        public string? RawResponse { get; set; }
    }

    /// <summary>
    /// Response for terminal status check
    /// </summary>
    public class TerminalStatusResponse : CommandResponseBase
    {
        public bool IsOnline { get; set; }
        public bool IsReady { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
        public string? FirmwareVersion { get; set; }
        public DateTime? LastActivity { get; set; }
        public Dictionary<string, object> DeviceStatus { get; set; } = new();
    }

    /// <summary>
    /// Response for fiscal receipt printing
    /// </summary>
    public class FiscalReceiptResponse : CommandResponseBase
    {
        public string? FiscalNumber { get; set; } // Unique fiscal receipt number
        public string? FiscalDate { get; set; }
        public string? FiscalTime { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalTax { get; set; }
        public int ReceiptNumber { get; set; }
        public string? CashRegisterId { get; set; }
    }

    /// <summary>
    /// Response for fiscal printer status check
    /// </summary>
    public class FiscalPrinterStatusResponse : CommandResponseBase
    {
        public bool IsOnline { get; set; }
        public bool HasPaper { get; set; }
        public bool FiscalMemoryOk { get; set; }
        public int? FiscalMemoryUsagePercent { get; set; }
        public DateTime? LastReceiptDate { get; set; }
        public string? LastFiscalNumber { get; set; }
        public bool IsInFiscalMode { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
        public string? FirmwareVersion { get; set; }
        public DateTime? LastZReportDate { get; set; }
    }

    /// <summary>
    /// Response for daily fiscal report
    /// </summary>
    public class FiscalReportResponse : CommandResponseBase
    {
        public DateTime ReportDate { get; set; }
        public string ReportType { get; set; } = "Daily"; // Daily, Periodic, Monthly
        public decimal TotalSales { get; set; }
        public decimal TotalTax { get; set; }
        public int ReceiptCount { get; set; }
        public Dictionary<string, decimal> SalesByTaxRate { get; set; } = new();
        public Dictionary<string, decimal> SalesByPaymentMethod { get; set; } = new();
        public string? ReportNumber { get; set; }
    }

    /// <summary>
    /// Generic success response for simple commands
    /// </summary>
    public class SimpleCommandResponse : CommandResponseBase
    {
        public string? Message { get; set; }
    }
}