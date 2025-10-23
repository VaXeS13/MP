using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MP.LocalAgent.Contracts.Models;

namespace MP.LocalAgent.Interfaces
{
    /// <summary>
    /// Service for managing Cumulative Revenue Register (CRK) - Polish fiscal compliance
    /// </summary>
    public interface ICRKService
    {
        /// <summary>
        /// Initialize CRK for a fiscal device
        /// </summary>
        Task<CumulativeRevenueRegister> InitializeCRKAsync(string fiscalDeviceId, string? fiscalPrinterModel = null);

        /// <summary>
        /// Record a transaction in the CRK
        /// </summary>
        Task<CRKTransaction> RecordTransactionAsync(Guid crkId, string receiptNumber, string transactionType,
            decimal amount, decimal taxAmount, string taxRate, string paymentMethod, string? notes = null);

        /// <summary>
        /// Get current CRK status
        /// </summary>
        Task<CRKStatus> GetCRKStatusAsync(Guid crkId);

        /// <summary>
        /// Get CRK by fiscal device ID
        /// </summary>
        Task<CumulativeRevenueRegister?> GetCRKByDeviceIdAsync(string fiscalDeviceId);

        /// <summary>
        /// Generate daily Z-report and update CRK
        /// </summary>
        Task<CRKDailySummary> GenerateZReportAsync(Guid crkId, DateTime reportDate);

        /// <summary>
        /// Get daily summary for specific date
        /// </summary>
        Task<CRKDailySummary?> GetDailySummaryAsync(Guid crkId, DateTime summaryDate);

        /// <summary>
        /// Verify CRK integrity and compliance
        /// </summary>
        Task<bool> VerifyCRKIntegrityAsync(Guid crkId);

        /// <summary>
        /// Get transaction history
        /// </summary>
        Task<List<CRKTransaction>> GetTransactionHistoryAsync(Guid crkId, DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Update CRK cumulative values after fiscal operation
        /// </summary>
        Task UpdateCumulativeValuesAsync(Guid crkId, decimal saleAmount, decimal taxAmount, string taxRate, string paymentMethod);

        /// <summary>
        /// Check if Z-report is required (daily closure)
        /// </summary>
        Task<bool> IsZReportRequiredAsync(Guid crkId);

        /// <summary>
        /// Calculate CRK hash for integrity verification
        /// </summary>
        Task<string> CalculateCRKHashAsync(Guid crkId);

        /// <summary>
        /// Export CRK data for audit/compliance purposes
        /// </summary>
        Task<string> ExportCRKDataAsync(Guid crkId, DateTime fromDate, DateTime toDate);
    }
}
