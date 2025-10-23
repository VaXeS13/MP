using System;
using System.Collections.Generic;

namespace MP.Application.Contracts.Devices;

/// <summary>
/// Terminal payment request for remote authorization
/// </summary>
public class TerminalPaymentRequest
{
    /// <summary>
    /// Amount to authorize (in smallest currency unit, e.g., cents)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code (e.g., "PLN", "EUR", "USD")
    /// </summary>
    public string CurrencyCode { get; set; } = "PLN";

    /// <summary>
    /// Transaction reference/ID (for tracking)
    /// </summary>
    public string? TransactionReference { get; set; }

    /// <summary>
    /// Payment description
    /// </summary>
    public string Description { get; set; } = "Booth Rental Payment";

    /// <summary>
    /// Timeout for operation in seconds (default 30)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Result of terminal payment operation
/// </summary>
public class TerminalPaymentResult
{
    /// <summary>
    /// Whether operation was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Terminal transaction ID
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// Authorization code from terminal
    /// </summary>
    public string? AuthorizationCode { get; set; }

    /// <summary>
    /// Amount authorized/captured
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Payment method used (Visa, Mastercard, etc.)
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code if operation failed
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Timestamp of operation
    /// </summary>
    public DateTime? OperationTime { get; set; }

    /// <summary>
    /// Card last 4 digits (for audit)
    /// </summary>
    public string? CardLast4Digits { get; set; }
}

/// <summary>
/// Fiscal receipt request
/// </summary>
public class FiscalReceiptRequest
{
    /// <summary>
    /// Receipt items (products/services sold)
    /// </summary>
    public List<FiscalReceiptItem> Items { get; set; } = new();

    /// <summary>
    /// Total amount (for validation)
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Payment method used
    /// </summary>
    public string PaymentMethod { get; set; } = "Card";

    /// <summary>
    /// Transaction ID for linking to payment
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// Receipt description/title
    /// </summary>
    public string Description { get; set; } = "Booth Rental Receipt";

    /// <summary>
    /// Additional notes/comments
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Timeout for operation in seconds (default 30)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Item on fiscal receipt
/// </summary>
public class FiscalReceiptItem
{
    /// <summary>
    /// Item description
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Quantity
    /// </summary>
    public decimal Quantity { get; set; } = 1;

    /// <summary>
    /// Unit price
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Tax rate (0-100)
    /// </summary>
    public decimal TaxRate { get; set; } = 23;

    /// <summary>
    /// Total amount (quantity * unit price)
    /// </summary>
    public decimal Total { get; set; }
}

/// <summary>
/// Result of fiscal receipt printing
/// </summary>
public class FiscalReceiptResult
{
    /// <summary>
    /// Whether operation was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Fiscal printer receipt ID
    /// </summary>
    public string? ReceiptId { get; set; }

    /// <summary>
    /// Receipt number
    /// </summary>
    public string? ReceiptNumber { get; set; }

    /// <summary>
    /// Fiscal device ID
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Timestamp of receipt printing
    /// </summary>
    public DateTime? PrintTime { get; set; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code if operation failed
    /// </summary>
    public string? ErrorCode { get; set; }
}

/// <summary>
/// Status of remote device
/// </summary>
public class RemoteDeviceStatus
{
    /// <summary>
    /// Device type (Terminal, FiscalPrinter, Scanner)
    /// </summary>
    public string DeviceType { get; set; } = string.Empty;

    /// <summary>
    /// Whether device is online and ready
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Whether device is currently busy
    /// </summary>
    public bool IsBusy { get; set; }

    /// <summary>
    /// Device serial number or ID
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Firmware version
    /// </summary>
    public string? FirmwareVersion { get; set; }

    /// <summary>
    /// Battery level (0-100) if applicable
    /// </summary>
    public int? BatteryLevel { get; set; }

    /// <summary>
    /// Last operation timestamp
    /// </summary>
    public DateTime? LastOperationTime { get; set; }

    /// <summary>
    /// Last error message
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Device connection status details
    /// </summary>
    public string? StatusMessage { get; set; }
}

/// <summary>
/// Terminal device capabilities
/// </summary>
public class TerminalCapabilities
{
    /// <summary>
    /// Supported card types (Visa, Mastercard, Amex, etc.)
    /// </summary>
    public List<string> SupportedCardTypes { get; set; } = new();

    /// <summary>
    /// Supported payment networks
    /// </summary>
    public List<string> SupportedNetworks { get; set; } = new();

    /// <summary>
    /// Whether terminal supports contactless payments
    /// </summary>
    public bool SupportsContactless { get; set; }

    /// <summary>
    /// Whether terminal supports EMV/Chip cards
    /// </summary>
    public bool SupportsEmv { get; set; }

    /// <summary>
    /// Whether terminal supports PIN entry
    /// </summary>
    public bool SupportsPinEntry { get; set; }

    /// <summary>
    /// Terminal model name
    /// </summary>
    public string? ModelName { get; set; }

    /// <summary>
    /// Serial number
    /// </summary>
    public string? SerialNumber { get; set; }
}

/// <summary>
/// Fiscal printer daily summary
/// </summary>
public class FiscalPrinterDailySummary
{
    /// <summary>
    /// Total receipts printed today
    /// </summary>
    public int TotalReceipts { get; set; }

    /// <summary>
    /// Total sales amount
    /// </summary>
    public decimal TotalSalesAmount { get; set; }

    /// <summary>
    /// Total tax amount
    /// </summary>
    public decimal TotalTaxAmount { get; set; }

    /// <summary>
    /// Total refunds
    /// </summary>
    public decimal TotalRefunds { get; set; }

    /// <summary>
    /// Opening balance
    /// </summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>
    /// Closing balance
    /// </summary>
    public decimal ClosingBalance { get; set; }

    /// <summary>
    /// Session number
    /// </summary>
    public string? SessionNumber { get; set; }

    /// <summary>
    /// Last receipt number
    /// </summary>
    public string? LastReceiptNumber { get; set; }

    /// <summary>
    /// Timestamp of summary
    /// </summary>
    public DateTime? SummaryTime { get; set; }
}
