using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using MP.Domain.Payments;
using MP.EntityFrameworkCore;

namespace MP.EntityFrameworkCore.Payments
{
    public class EfCorePayPalTransactionRepository : EfCoreRepository<MPDbContext, PayPalTransaction, Guid>, IPayPalTransactionRepository
    {
        public EfCorePayPalTransactionRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<PayPalTransaction?> GetByOrderIdAsync(string orderId)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.FirstOrDefaultAsync(t => t.OrderId == orderId);
        }

        public async Task<PayPalTransaction?> FindBySessionIdAsync(string sessionId)
        {
            // Session ID is the Order ID for PayPal
            return await GetByOrderIdAsync(sessionId);
        }

        public async Task<PayPalTransaction?> GetByPaymentIdAsync(string paymentId)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.FirstOrDefaultAsync(t => t.PaymentId == paymentId);
        }

        public async Task<PayPalTransaction?> GetByCaptureIdAsync(string captureId)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.FirstOrDefaultAsync(t => t.CaptureId == captureId);
        }

        public async Task<List<PayPalTransaction>> GetByRentalIdAsync(Guid rentalId)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(t => t.RentalId == rentalId)
                .OrderByDescending(t => t.CreationTime)
                .ToListAsync();
        }

        public async Task<List<PayPalTransaction>> GetByEmailAsync(string email)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(t => t.Email == email)
                .OrderByDescending(t => t.CreationTime)
                .ToListAsync();
        }

        public async Task<List<PayPalTransaction>> GetByPayerIdAsync(string payerId)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(t => t.PayerId == payerId)
                .OrderByDescending(t => t.CreationTime)
                .ToListAsync();
        }

        public async Task<List<PayPalTransaction>> GetByStatusAsync(string status)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.CreationTime)
                .ToListAsync();
        }

        public async Task<List<PayPalTransaction>> GetPendingStatusChecksAsync(DateTime olderThan, int maxCount = 100)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(t =>
                    (t.Status == "CREATED" || t.Status == "APPROVED" || t.Status == "PAYER_ACTION_REQUIRED") &&
                    (t.LastStatusCheck == null || t.LastStatusCheck < olderThan) &&
                    t.StatusCheckCount < 10) // Prevent infinite loops
                .OrderBy(t => t.LastStatusCheck ?? t.CreationTime)
                .Take(maxCount)
                .ToListAsync();
        }

        public async Task<List<PayPalTransaction>> GetCompletedTransactionsAsync(DateTime fromDate, DateTime toDate)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(t =>
                    t.Status == "COMPLETED" &&
                    t.CompletedAt >= fromDate &&
                    t.CompletedAt <= toDate)
                .OrderByDescending(t => t.CompletedAt)
                .ToListAsync();
        }

        public async Task<List<PayPalTransaction>> GetByEnvironmentAsync(string environment)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(t => t.Environment == environment)
                .OrderByDescending(t => t.CreationTime)
                .ToListAsync();
        }

        public async Task<List<PayPalTransaction>> GetDisputedTransactionsAsync()
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(t => !string.IsNullOrEmpty(t.DisputeId))
                .OrderByDescending(t => t.CreationTime)
                .ToListAsync();
        }

        public async Task<List<PayPalTransaction>> GetRefundedTransactionsAsync()
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(t => !string.IsNullOrEmpty(t.RefundId))
                .OrderByDescending(t => t.CreationTime)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalAmountAsync(DateTime fromDate, DateTime toDate, Guid? tenantId = null)
        {
            var dbSet = await GetDbSetAsync();
            var query = dbSet
                .Where(t =>
                    t.Status == "COMPLETED" &&
                    t.CompletedAt >= fromDate &&
                    t.CompletedAt <= toDate);

            if (tenantId.HasValue)
            {
                query = query.Where(t => t.TenantId == tenantId);
            }

            var totalAmount = await query.SumAsync(t => t.Amount);
            return totalAmount;
        }

        public async Task<int> GetCancelledTransactionsCountAsync(DateTime fromDate, DateTime toDate)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .CountAsync(t =>
                    (t.Status == "VOIDED" || t.Status == "CANCELLED") &&
                    t.CreationTime >= fromDate &&
                    t.CreationTime <= toDate);
        }

        public async Task<List<PayPalTransaction>> GetByFundingSourceAsync(string fundingSource)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(t => t.FundingSource == fundingSource)
                .OrderByDescending(t => t.CreationTime)
                .ToListAsync();
        }

        public async Task<decimal> GetAverageTransactionAmountAsync(DateTime fromDate, DateTime toDate)
        {
            var dbSet = await GetDbSetAsync();
            var averageAmount = await dbSet
                .Where(t =>
                    t.Status == "COMPLETED" &&
                    t.CompletedAt >= fromDate &&
                    t.CompletedAt <= toDate)
                .AverageAsync(t => t.Amount);

            return averageAmount;
        }

        public async Task<int> GetFailedTransactionsCountAsync(DateTime fromDate, DateTime toDate)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .CountAsync(t =>
                    (t.Status == "VOIDED" || t.Status == "CANCELLED" || t.Status == "FAILED") &&
                    t.CreationTime >= fromDate &&
                    t.CreationTime <= toDate);
        }
    }
}