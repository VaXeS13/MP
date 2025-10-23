using System;
using System.Collections.Generic;

namespace MP.LocalAgent.Contracts.Models
{
    /// <summary>
    /// Cumulative Revenue Register (CRK) - Polish fiscal compliance data
    /// </summary>
    public class CumulativeRevenueRegister
    {
        /// <summary>
        /// Unique CRK identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Fiscal device identifier (serial number or device ID)
        /// </summary>
        public string FiscalDeviceId { get; set; } = string.Empty;

        /// <summary>
        /// Cumulative total revenue (suma sprzedaży)
        /// </summary>
        public decimal CumulativeRevenue { get; set; }

        /// <summary>
        /// Cumulative tax amount (suma VAT)
        /// </summary>
        public decimal CumulativeTaxAmount { get; set; }

        /// <summary>
        /// Cumulative refund amount (suma zwrotów)
        /// </summary>
        public decimal CumulativeRefundAmount { get; set; }

        /// <summary>
        /// Total number of receipts issued (ilość paragonów)
        /// </summary>
        public long TotalReceiptCount { get; set; }

        /// <summary>
        /// Last receipt number issued
        /// </summary>
        public string? LastReceiptNumber { get; set; }

        /// <summary>
        /// Cumulative turnover for each tax rate (A, B, C, etc.)
        /// Format: TaxRate -> Amount
        /// </summary>
        public Dictionary<string, decimal> CumulativeTurnoverByTaxRate { get; set; } = new();

        /// <summary>
        /// Cumulative sales by payment method
        /// Format: PaymentMethod -> Amount
        /// </summary>
        public Dictionary<string, decimal> CumulativeSalesByPaymentMethod { get; set; } = new();

        /// <summary>
        /// Period start date
        /// </summary>
        public DateTime PeriodStartDate { get; set; }

        /// <summary>
        /// Period end date (null if current period)
        /// </summary>
        public DateTime? PeriodEndDate { get; set; }

        /// <summary>
        /// Last receipt date/time
        /// </summary>
        public DateTime? LastReceiptDateTime { get; set; }

        /// <summary>
        /// Last Z-report date (daily closure)
        /// </summary>
        public DateTime? LastZReportDateTime { get; set; }

        /// <summary>
        /// Z-report counter (daily closure counter)
        /// </summary>
        public int ZReportCounter { get; set; }

        /// <summary>
        /// Whether CRK is in fiscal mode
        /// </summary>
        public bool IsInFiscalMode { get; set; }

        /// <summary>
        /// CRK hash for integrity verification
        /// </summary>
        public string? CRKHash { get; set; }

        /// <summary>
        /// Timestamp of last CRK update
        /// </summary>
        public DateTime LastUpdatedAt { get; set; }

        /// <summary>
        /// Region code (e.g., "PL" for Poland)
        /// </summary>
        public string RegionCode { get; set; } = "PL";

        /// <summary>
        /// Fiscal printer model/vendor
        /// </summary>
        public string? FiscalPrinterModel { get; set; }

        /// <summary>
        /// Provider-specific metadata
        /// </summary>
        public Dictionary<string, object> ProviderData { get; set; } = new();
    }

    /// <summary>
    /// CRK transaction log entry
    /// </summary>
    public class CRKTransaction
    {
        /// <summary>
        /// Transaction identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// CRK identifier this transaction belongs to
        /// </summary>
        public Guid CRKId { get; set; }

        /// <summary>
        /// Receipt number associated with transaction
        /// </summary>
        public string ReceiptNumber { get; set; } = string.Empty;

        /// <summary>
        /// Transaction type (Sale, Refund, Correction, etc.)
        /// </summary>
        public string TransactionType { get; set; } = "Sale";

        /// <summary>
        /// Transaction amount (before tax)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Tax amount for transaction
        /// </summary>
        public decimal TaxAmount { get; set; }

        /// <summary>
        /// Total amount (amount + tax)
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Tax rate applied (A=23%, B=8%, C=5%, D=0%)
        /// </summary>
        public string TaxRate { get; set; } = "A";

        /// <summary>
        /// Payment method used
        /// </summary>
        public string PaymentMethod { get; set; } = "Card";

        /// <summary>
        /// Transaction date/time
        /// </summary>
        public DateTime TransactionDateTime { get; set; }

        /// <summary>
        /// Cumulative revenue after this transaction
        /// </summary>
        public decimal CumulativeRevenueAfter { get; set; }

        /// <summary>
        /// Transaction sequence number
        /// </summary>
        public long SequenceNumber { get; set; }

        /// <summary>
        /// Related fiscal receipt ID (if available)
        /// </summary>
        public string? FiscalReceiptId { get; set; }

        /// <summary>
        /// Transaction status (Completed, Cancelled, etc.)
        /// </summary>
        public string Status { get; set; } = "Completed";

        /// <summary>
        /// Transaction notes/description
        /// </summary>
        public string? Notes { get; set; }
    }

    /// <summary>
    /// CRK daily summary/Z-Report
    /// </summary>
    public class CRKDailySummary
    {
        /// <summary>
        /// Summary identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// CRK identifier this summary belongs to
        /// </summary>
        public Guid CRKId { get; set; }

        /// <summary>
        /// Summary date
        /// </summary>
        public DateTime SummaryDate { get; set; }

        /// <summary>
        /// Z-report number
        /// </summary>
        public int ZReportNumber { get; set; }

        /// <summary>
        /// Opening balance
        /// </summary>
        public decimal OpeningBalance { get; set; }

        /// <summary>
        /// Daily sales total
        /// </summary>
        public decimal DailySalesTotal { get; set; }

        /// <summary>
        /// Daily tax total
        /// </summary>
        public decimal DailyTaxTotal { get; set; }

        /// <summary>
        /// Daily refunds total
        /// </summary>
        public decimal DailyRefundsTotal { get; set; }

        /// <summary>
        /// Closing balance
        /// </summary>
        public decimal ClosingBalance { get; set; }

        /// <summary>
        /// Number of receipts issued today
        /// </summary>
        public int ReceiptCountToday { get; set; }

        /// <summary>
        /// Sales by tax rate for the day
        /// </summary>
        public Dictionary<string, decimal> SalesByTaxRateToday { get; set; } = new();

        /// <summary>
        /// Sales by payment method for the day
        /// </summary>
        public Dictionary<string, decimal> SalesByPaymentMethodToday { get; set; } = new();

        /// <summary>
        /// Z-report generation time
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// Z-report hash for integrity verification
        /// </summary>
        public string? ZReportHash { get; set; }

        /// <summary>
        /// Status of Z-report (Generated, Transmitted, Verified, etc.)
        /// </summary>
        public string Status { get; set; } = "Generated";
    }

    /// <summary>
    /// CRK status information
    /// </summary>
    public class CRKStatus
    {
        /// <summary>
        /// Whether CRK is active and in fiscal mode
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Current cumulative revenue
        /// </summary>
        public decimal CurrentCumulativeRevenue { get; set; }

        /// <summary>
        /// Current cumulative tax
        /// </summary>
        public decimal CurrentCumulativeTax { get; set; }

        /// <summary>
        /// Total receipts issued
        /// </summary>
        public long TotalReceiptsIssued { get; set; }

        /// <summary>
        /// Last receipt number
        /// </summary>
        public string? LastReceiptNumber { get; set; }

        /// <summary>
        /// Last transaction date/time
        /// </summary>
        public DateTime? LastTransactionDateTime { get; set; }

        /// <summary>
        /// Days since last Z-report
        /// </summary>
        public int DaysSinceLastZReport { get; set; }

        /// <summary>
        /// Is CRK compliant with Polish regulations
        /// </summary>
        public bool IsCompliant { get; set; }

        /// <summary>
        /// Last compliance check date
        /// </summary>
        public DateTime? LastComplianceCheckDate { get; set; }

        /// <summary>
        /// Compliance warnings/issues
        /// </summary>
        public List<string> ComplianceWarnings { get; set; } = new();
    }
}
