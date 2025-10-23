using System;
using MP.LocalAgent.Contracts.Exceptions;

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

        // ✅ PCI DSS Compliance fields
        /// <summary>
        /// Masked PAN (Primary Account Number) - only last 4 digits
        /// Format: ****1234 (NEVER store full card number!)
        /// </summary>
        public string? MaskedPan { get; set; }

        /// <summary>
        /// Indicates if terminal is Point-to-Point Encryption (P2PE) certified
        /// </summary>
        public bool IsP2PECompliant { get; set; } = true;

        /// <summary>
        /// Safe metadata that does NOT contain sensitive card data
        /// Example: { "TerminalId": "TERM-001", "ProcessingTime": "2500ms" }
        /// </summary>
        public Dictionary<string, string> SafeMetadata { get; set; } = new();

        /// <summary>
        /// Validate response for PCI DSS compliance
        /// Throws PciComplianceException if PAN data is exposed
        /// </summary>
        public void ValidatePciCompliance()
        {
            // Check if MaskedPan contains more than 4 digits (which would be unsafe)
            if (!string.IsNullOrEmpty(MaskedPan))
            {
                var digitCount = MaskedPan.Count(char.IsDigit);
                if (digitCount > 4)
                {
                    throw new PciComplianceException(
                        $"PCI DSS Violation: MaskedPan contains {digitCount} digits. " +
                        "Only last 4 digits (****1234) are allowed.");
                }
            }

            // Ensure P2PE compliance
            if (!IsP2PECompliant)
            {
                throw new PciComplianceException(
                    "Terminal is not P2PE certified. Cannot process payments.");
            }
        }
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