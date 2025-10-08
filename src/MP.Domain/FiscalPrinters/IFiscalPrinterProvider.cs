using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MP.Domain.Terminals.Communication;

namespace MP.Domain.FiscalPrinters
{
    /// <summary>
    /// Interface for fiscal printer providers
    /// Handles receipt printing with fiscal memory and tax compliance
    /// </summary>
    public interface IFiscalPrinterProvider
    {
        /// <summary>
        /// Unique identifier for the fiscal printer provider
        /// </summary>
        string ProviderId { get; }

        /// <summary>
        /// Display name
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Supported countries/regions
        /// </summary>
        string[] SupportedRegions { get; }

        /// <summary>
        /// Initialize printer with configuration
        /// </summary>
        Task InitializeAsync(FiscalPrinterSettings settings);

        /// <summary>
        /// Check if printer is online and ready
        /// </summary>
        Task<FiscalPrinterStatus> GetStatusAsync();

        /// <summary>
        /// Print fiscal receipt
        /// </summary>
        Task<FiscalReceiptResult> PrintReceiptAsync(FiscalReceiptRequest request);

        /// <summary>
        /// Print non-fiscal document (invoice copy, report, etc.)
        /// </summary>
        Task<bool> PrintNonFiscalAsync(string[] lines);

        /// <summary>
        /// Get daily sales report (Z-report)
        /// </summary>
        Task<FiscalReportResult> GetDailyReportAsync(DateTime date);

        /// <summary>
        /// Cancel last receipt (if allowed by fiscal law)
        /// </summary>
        Task<bool> CancelLastReceiptAsync(string reason);
    }

    /// <summary>
    /// Fiscal printer connection and configuration settings
    /// </summary>
    public class FiscalPrinterSettings
    {
        public string ProviderId { get; set; } = null!;
        public bool IsEnabled { get; set; }
        public string Region { get; set; } = "PL";
        public TerminalConnectionSettings ConnectionSettings { get; set; } = new();

        // Fiscal data
        public string? TaxId { get; set; } // NIP in Poland, VAT ID in EU
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? CashierName { get; set; }

        // Tax rates configuration
        public Dictionary<string, decimal> TaxRates { get; set; } = new();

        // Printer-specific config
        public Dictionary<string, object> ProviderConfig { get; set; } = new();
    }

    /// <summary>
    /// Fiscal receipt request
    /// </summary>
    public class FiscalReceiptRequest
    {
        public string TransactionId { get; set; } = null!;
        public List<FiscalReceiptItem> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, Mixed
        public decimal? CashPaid { get; set; }
        public decimal? CardPaid { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerTaxId { get; set; }
        public string? CashierName { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Single item on fiscal receipt
    /// </summary>
    public class FiscalReceiptItem
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string TaxRate { get; set; } = "A"; // A, B, C, D, E (country-specific)
        public string? Barcode { get; set; }
        public string? SKU { get; set; }
    }

    /// <summary>
    /// Result of fiscal receipt printing
    /// </summary>
    public class FiscalReceiptResult
    {
        public bool Success { get; set; }
        public string? FiscalNumber { get; set; } // Unique fiscal receipt number
        public string? FiscalDate { get; set; }
        public string? FiscalTime { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalTax { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
        public Dictionary<string, object> ProviderData { get; set; } = new();
    }

    /// <summary>
    /// Fiscal printer status
    /// </summary>
    public class FiscalPrinterStatus
    {
        public bool IsOnline { get; set; }
        public bool HasPaper { get; set; }
        public bool FiscalMemoryOk { get; set; }
        public int? FiscalMemoryUsagePercent { get; set; }
        public DateTime? LastReceiptDate { get; set; }
        public string? LastFiscalNumber { get; set; }
        public bool IsInFiscalMode { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> ProviderData { get; set; } = new();
    }

    /// <summary>
    /// Fiscal report (daily/periodic)
    /// </summary>
    public class FiscalReportResult
    {
        public DateTime ReportDate { get; set; }
        public string ReportType { get; set; } = "Daily"; // Daily, Periodic, Monthly
        public decimal TotalSales { get; set; }
        public decimal TotalTax { get; set; }
        public int ReceiptCount { get; set; }
        public Dictionary<string, decimal> SalesByTaxRate { get; set; } = new();
        public Dictionary<string, decimal> SalesByPaymentMethod { get; set; } = new();
        public Dictionary<string, object> ProviderData { get; set; } = new();
    }
}
