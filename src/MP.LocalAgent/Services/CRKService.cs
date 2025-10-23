using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MP.LocalAgent.Contracts.Models;
using MP.LocalAgent.Interfaces;

namespace MP.LocalAgent.Services
{
    /// <summary>
    /// Implementation of Cumulative Revenue Register (CRK) for Polish fiscal compliance
    /// </summary>
    public class CRKService : ICRKService
    {
        private readonly ILogger<CRKService> _logger;
        // In-memory storage for CRK data (in production, this would use a database)
        private readonly Dictionary<Guid, CumulativeRevenueRegister> _crkRegistry = new();
        private readonly Dictionary<Guid, List<CRKTransaction>> _transactions = new();
        private readonly Dictionary<Guid, List<CRKDailySummary>> _dailySummaries = new();

        // Z-report requirement: every 24 hours for Polish tax compliance
        private const int HOURS_BETWEEN_ZREPORTS = 24;

        public CRKService(ILogger<CRKService> logger)
        {
            _logger = logger;
        }

        public async Task<CumulativeRevenueRegister> InitializeCRKAsync(string fiscalDeviceId, string? fiscalPrinterModel = null)
        {
            try
            {
                var crk = new CumulativeRevenueRegister
                {
                    Id = Guid.NewGuid(),
                    FiscalDeviceId = fiscalDeviceId,
                    CumulativeRevenue = 0,
                    CumulativeTaxAmount = 0,
                    CumulativeRefundAmount = 0,
                    TotalReceiptCount = 0,
                    PeriodStartDate = DateTime.UtcNow,
                    IsInFiscalMode = true,
                    ZReportCounter = 0,
                    LastUpdatedAt = DateTime.UtcNow,
                    FiscalPrinterModel = fiscalPrinterModel
                };

                _crkRegistry[crk.Id] = crk;
                _transactions[crk.Id] = new List<CRKTransaction>();
                _dailySummaries[crk.Id] = new List<CRKDailySummary>();

                // Calculate initial CRK hash
                crk.CRKHash = await CalculateCRKHashAsync(crk.Id);

                _logger.LogInformation("CRK initialized for device {FiscalDeviceId} with CRK ID {CRKId}",
                    fiscalDeviceId, crk.Id);

                return crk;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing CRK for device {FiscalDeviceId}", fiscalDeviceId);
                throw;
            }
        }

        public async Task<CumulativeRevenueRegister?> GetCRKByDeviceIdAsync(string fiscalDeviceId)
        {
            return await Task.FromResult(_crkRegistry.Values.FirstOrDefault(c => c.FiscalDeviceId == fiscalDeviceId));
        }

        public async Task<CRKTransaction> RecordTransactionAsync(Guid crkId, string receiptNumber, string transactionType,
            decimal amount, decimal taxAmount, string taxRate, string paymentMethod, string? notes = null)
        {
            try
            {
                if (!_crkRegistry.TryGetValue(crkId, out var crk))
                {
                    throw new InvalidOperationException($"CRK with ID {crkId} not found");
                }

                var transaction = new CRKTransaction
                {
                    Id = Guid.NewGuid(),
                    CRKId = crkId,
                    ReceiptNumber = receiptNumber,
                    TransactionType = transactionType,
                    Amount = amount,
                    TaxAmount = taxAmount,
                    TotalAmount = amount + taxAmount,
                    TaxRate = taxRate,
                    PaymentMethod = paymentMethod,
                    TransactionDateTime = DateTime.UtcNow,
                    SequenceNumber = _transactions[crkId].Count + 1,
                    Status = "Completed",
                    Notes = notes
                };

                // Update cumulative values
                await UpdateCumulativeValuesAsync(crkId, amount, taxAmount, taxRate, paymentMethod);

                // Set cumulative revenue after transaction
                crk = _crkRegistry[crkId];
                transaction.CumulativeRevenueAfter = crk.CumulativeRevenue;

                _transactions[crkId].Add(transaction);

                // Update CRK receipt tracking
                crk.TotalReceiptCount++;
                crk.LastReceiptNumber = receiptNumber;
                crk.LastReceiptDateTime = transaction.TransactionDateTime;
                crk.LastUpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Transaction recorded - CRK: {CRKId}, Receipt: {ReceiptNumber}, Amount: {Amount}, Tax: {TaxAmount}",
                    crkId, receiptNumber, amount, taxAmount);

                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording transaction for CRK {CRKId}", crkId);
                throw;
            }
        }

        public async Task<CRKStatus> GetCRKStatusAsync(Guid crkId)
        {
            try
            {
                if (!_crkRegistry.TryGetValue(crkId, out var crk))
                {
                    throw new InvalidOperationException($"CRK with ID {crkId} not found");
                }

                var lastZReport = _dailySummaries[crkId].OrderByDescending(ds => ds.SummaryDate).FirstOrDefault();
                var daysSinceLastZReport = lastZReport != null
                    ? (int)(DateTime.UtcNow - lastZReport.GeneratedAt).TotalDays
                    : (int)(DateTime.UtcNow - crk.PeriodStartDate).TotalDays;

                var complianceWarnings = new List<string>();

                // Check compliance requirements
                if (daysSinceLastZReport > HOURS_BETWEEN_ZREPORTS / 24)
                {
                    complianceWarnings.Add($"Z-report required (last one {daysSinceLastZReport} days ago)");
                }

                if (crk.CumulativeTaxAmount < 0)
                {
                    complianceWarnings.Add("Negative tax amount detected");
                }

                var status = new CRKStatus
                {
                    IsActive = crk.IsInFiscalMode,
                    CurrentCumulativeRevenue = crk.CumulativeRevenue,
                    CurrentCumulativeTax = crk.CumulativeTaxAmount,
                    TotalReceiptsIssued = crk.TotalReceiptCount,
                    LastReceiptNumber = crk.LastReceiptNumber,
                    LastTransactionDateTime = crk.LastReceiptDateTime,
                    DaysSinceLastZReport = daysSinceLastZReport,
                    IsCompliant = complianceWarnings.Count == 0,
                    LastComplianceCheckDate = DateTime.UtcNow,
                    ComplianceWarnings = complianceWarnings
                };

                return await Task.FromResult(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting CRK status for {CRKId}", crkId);
                throw;
            }
        }

        public async Task<CRKDailySummary> GenerateZReportAsync(Guid crkId, DateTime reportDate)
        {
            try
            {
                if (!_crkRegistry.TryGetValue(crkId, out var crk))
                {
                    throw new InvalidOperationException($"CRK with ID {crkId} not found");
                }

                var dateOnly = reportDate.Date;
                var dayTransactions = _transactions[crkId]
                    .Where(t => t.TransactionDateTime.Date == dateOnly && t.Status == "Completed")
                    .ToList();

                var salesByTaxRate = new Dictionary<string, decimal>();
                var salesByPaymentMethod = new Dictionary<string, decimal>();

                foreach (var transaction in dayTransactions)
                {
                    // Aggregate by tax rate
                    if (!salesByTaxRate.ContainsKey(transaction.TaxRate))
                        salesByTaxRate[transaction.TaxRate] = 0;
                    salesByTaxRate[transaction.TaxRate] += transaction.Amount;

                    // Aggregate by payment method
                    if (!salesByPaymentMethod.ContainsKey(transaction.PaymentMethod))
                        salesByPaymentMethod[transaction.PaymentMethod] = 0;
                    salesByPaymentMethod[transaction.PaymentMethod] += transaction.Amount;
                }

                var dailySalesTotal = dayTransactions.Sum(t => t.Amount);
                var dailyTaxTotal = dayTransactions.Sum(t => t.TaxAmount);
                var dailyRefunds = dayTransactions.Where(t => t.TransactionType == "Refund").Sum(t => t.Amount);

                var summary = new CRKDailySummary
                {
                    Id = Guid.NewGuid(),
                    CRKId = crkId,
                    SummaryDate = dateOnly,
                    ZReportNumber = ++crk.ZReportCounter,
                    OpeningBalance = crk.CumulativeRevenue - dailySalesTotal + dailyRefunds,
                    DailySalesTotal = dailySalesTotal,
                    DailyTaxTotal = dailyTaxTotal,
                    DailyRefundsTotal = dailyRefunds,
                    ClosingBalance = crk.CumulativeRevenue,
                    ReceiptCountToday = dayTransactions.Count,
                    SalesByTaxRateToday = salesByTaxRate,
                    SalesByPaymentMethodToday = salesByPaymentMethod,
                    GeneratedAt = DateTime.UtcNow,
                    Status = "Generated"
                };

                // Calculate Z-report hash
                summary.ZReportHash = CalculateZReportHash(summary);

                _dailySummaries[crkId].Add(summary);

                // Update CRK
                crk.LastZReportDateTime = DateTime.UtcNow;
                crk.LastUpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Z-report generated - CRK: {CRKId}, ZReport#: {ZReportNumber}, DailySalesTotal: {DailySalesTotal}",
                    crkId, summary.ZReportNumber, dailySalesTotal);

                return await Task.FromResult(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Z-report for CRK {CRKId}", crkId);
                throw;
            }
        }

        public async Task<CRKDailySummary?> GetDailySummaryAsync(Guid crkId, DateTime summaryDate)
        {
            if (!_dailySummaries.TryGetValue(crkId, out var summaries))
            {
                return null;
            }

            return await Task.FromResult(summaries.FirstOrDefault(s => s.SummaryDate.Date == summaryDate.Date));
        }

        public async Task<bool> VerifyCRKIntegrityAsync(Guid crkId)
        {
            try
            {
                if (!_crkRegistry.TryGetValue(crkId, out var crk))
                {
                    return false;
                }

                var currentHash = await CalculateCRKHashAsync(crkId);
                var isValid = crk.CRKHash == currentHash;

                if (!isValid)
                {
                    _logger.LogWarning("CRK integrity verification failed for {CRKId}. Expected: {ExpectedHash}, Got: {CurrentHash}",
                        crkId, crk.CRKHash, currentHash);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying CRK integrity for {CRKId}", crkId);
                return false;
            }
        }

        public async Task<List<CRKTransaction>> GetTransactionHistoryAsync(Guid crkId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                if (!_transactions.TryGetValue(crkId, out var transactions))
                {
                    return new List<CRKTransaction>();
                }

                var result = transactions.AsEnumerable();

                if (fromDate.HasValue)
                {
                    result = result.Where(t => t.TransactionDateTime >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    result = result.Where(t => t.TransactionDateTime <= toDate.Value);
                }

                return await Task.FromResult(result.OrderBy(t => t.TransactionDateTime).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction history for CRK {CRKId}", crkId);
                return new List<CRKTransaction>();
            }
        }

        public async Task UpdateCumulativeValuesAsync(Guid crkId, decimal saleAmount, decimal taxAmount, string taxRate, string paymentMethod)
        {
            try
            {
                if (!_crkRegistry.TryGetValue(crkId, out var crk))
                {
                    throw new InvalidOperationException($"CRK with ID {crkId} not found");
                }

                crk.CumulativeRevenue += saleAmount;
                crk.CumulativeTaxAmount += taxAmount;

                // Update turnover by tax rate
                if (!crk.CumulativeTurnoverByTaxRate.ContainsKey(taxRate))
                {
                    crk.CumulativeTurnoverByTaxRate[taxRate] = 0;
                }
                crk.CumulativeTurnoverByTaxRate[taxRate] += saleAmount;

                // Update sales by payment method
                if (!crk.CumulativeSalesByPaymentMethod.ContainsKey(paymentMethod))
                {
                    crk.CumulativeSalesByPaymentMethod[paymentMethod] = 0;
                }
                crk.CumulativeSalesByPaymentMethod[paymentMethod] += saleAmount;

                crk.LastUpdatedAt = DateTime.UtcNow;

                // Recalculate hash
                crk.CRKHash = await CalculateCRKHashAsync(crkId);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cumulative values for CRK {CRKId}", crkId);
                throw;
            }
        }

        public async Task<bool> IsZReportRequiredAsync(Guid crkId)
        {
            try
            {
                if (!_crkRegistry.TryGetValue(crkId, out var crk))
                {
                    return false;
                }

                if (!crk.LastZReportDateTime.HasValue)
                {
                    // First Z-report required after 24 hours
                    return (DateTime.UtcNow - crk.PeriodStartDate).TotalHours >= HOURS_BETWEEN_ZREPORTS;
                }

                // Z-report required every 24 hours
                return (DateTime.UtcNow - crk.LastZReportDateTime.Value).TotalHours >= HOURS_BETWEEN_ZREPORTS;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Z-report requirement for CRK {CRKId}", crkId);
                return false;
            }
        }

        public async Task<string> CalculateCRKHashAsync(Guid crkId)
        {
            try
            {
                if (!_crkRegistry.TryGetValue(crkId, out var crk))
                {
                    throw new InvalidOperationException($"CRK with ID {crkId} not found");
                }

                // Create hashable representation of CRK state
                var hashInput = new StringBuilder();
                hashInput.Append($"CRKId:{crk.Id}");
                hashInput.Append($"|Device:{crk.FiscalDeviceId}");
                hashInput.Append($"|Revenue:{crk.CumulativeRevenue}");
                hashInput.Append($"|Tax:{crk.CumulativeTaxAmount}");
                hashInput.Append($"|Receipts:{crk.TotalReceiptCount}");
                hashInput.Append($"|LastReceipt:{crk.LastReceiptNumber}");

                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput.ToString()));
                return await Task.FromResult(Convert.ToHexString(hash));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating CRK hash for {CRKId}", crkId);
                throw;
            }
        }

        public async Task<string> ExportCRKDataAsync(Guid crkId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                if (!_crkRegistry.TryGetValue(crkId, out var crk))
                {
                    throw new InvalidOperationException($"CRK with ID {crkId} not found");
                }

                var transactions = await GetTransactionHistoryAsync(crkId, fromDate, toDate);
                var summaries = _dailySummaries[crkId]
                    .Where(s => s.SummaryDate >= fromDate.Date && s.SummaryDate <= toDate.Date)
                    .ToList();

                var exportData = new
                {
                    CRK = crk,
                    Transactions = transactions,
                    DailySummaries = summaries,
                    ExportDate = DateTime.UtcNow,
                    ExportedFrom = fromDate,
                    ExportedTo = toDate
                };

                return await Task.FromResult(JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting CRK data for {CRKId}", crkId);
                throw;
            }
        }

        private string CalculateZReportHash(CRKDailySummary summary)
        {
            var hashInput = new StringBuilder();
            hashInput.Append($"ZReport:{summary.ZReportNumber}");
            hashInput.Append($"|Date:{summary.SummaryDate:yyyy-MM-dd}");
            hashInput.Append($"|DailySales:{summary.DailySalesTotal}");
            hashInput.Append($"|DailyTax:{summary.DailyTaxTotal}");
            hashInput.Append($"|ClosingBalance:{summary.ClosingBalance}");

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput.ToString()));
            return Convert.ToHexString(hash);
        }
    }
}
