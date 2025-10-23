using System;
using System.Collections.Generic;

namespace MP.LocalAgent.Contracts.Commands
{
    /// <summary>
    /// Command to print fiscal receipt
    /// </summary>
    public class PrintFiscalReceiptCommand
    {
        public Guid CommandId { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string FiscalPrinterProviderId { get; set; } = null!;
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
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Command to print non-fiscal document
    /// </summary>
    public class PrintNonFiscalDocumentCommand
    {
        public Guid CommandId { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string FiscalPrinterProviderId { get; set; } = null!;
        public string[] Lines { get; set; } = Array.Empty<string>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Command to get daily sales report (Z-report)
    /// </summary>
    public class GetDailyFiscalReportCommand
    {
        public Guid CommandId { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string FiscalPrinterProviderId { get; set; } = null!;
        public DateTime ReportDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);
    }

    /// <summary>
    /// Command to cancel last receipt
    /// </summary>
    public class CancelLastFiscalReceiptCommand
    {
        public Guid CommandId { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string FiscalPrinterProviderId { get; set; } = null!;
        public string Reason { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Command to check fiscal printer status
    /// </summary>
    public class CheckFiscalPrinterStatusCommand
    {
        public Guid CommandId { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string FiscalPrinterProviderId { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Fiscal receipt item data
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
}