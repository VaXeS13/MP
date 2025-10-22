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
    public class EfCoreStripeTransactionRepository : EfCoreRepository<MPDbContext, StripeTransaction, Guid>, IStripeTransactionRepository
    {
        public EfCoreStripeTransactionRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<StripeTransaction?> GetByPaymentIntentIdAsync(string paymentIntentId)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.FirstOrDefaultAsync(t => t.PaymentIntentId == paymentIntentId);
        }

        public async Task<StripeTransaction?> FindBySessionIdAsync(string sessionId)
        {
            // Session ID is stored in PaymentIntentId field (Checkout Session ID)
            return await GetByPaymentIntentIdAsync(sessionId);
        }

        public async Task<List<StripeTransaction>> GetByRentalIdAsync(Guid rentalId)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(t => t.RentalId == rentalId)
                .OrderByDescending(t => t.CreationTime)
                .ToListAsync();
        }

        public async Task<List<StripeTransaction>> GetByEmailAsync(string email)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(t => t.Email == email)
                .OrderByDescending(t => t.CreationTime)
                .ToListAsync();
        }

        public async Task<List<StripeTransaction>> GetByStatusAsync(string status)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.CreationTime)
                .ToListAsync();
        }

        public async Task<List<StripeTransaction>> GetPendingStatusChecksAsync(DateTime olderThan, int maxCount = 100)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(t =>
                    (t.Status == "requires_payment_method" || t.Status == "requires_confirmation" || t.Status == "processing") &&
                    (t.LastStatusCheck == null || t.LastStatusCheck < olderThan) &&
                    t.StatusCheckCount < 10) // Prevent infinite loops
                .OrderBy(t => t.LastStatusCheck ?? t.CreationTime)
                .Take(maxCount)
                .ToListAsync();
        }

        public async Task<List<StripeTransaction>> GetSuccessfulTransactionsAsync(DateTime fromDate, DateTime toDate)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(t =>
                    t.Status == "succeeded" &&
                    t.CompletedAt >= fromDate &&
                    t.CompletedAt <= toDate)
                .OrderByDescending(t => t.CompletedAt)
                .ToListAsync();
        }

        public async Task<StripeTransaction?> GetByChargeIdAsync(string chargeId)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.FirstOrDefaultAsync(t => t.ChargeId == chargeId);
        }

        public async Task<List<StripeTransaction>> GetByCustomerIdAsync(string customerId)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(t => t.CustomerId == customerId)
                .OrderByDescending(t => t.CreationTime)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalAmountAsync(DateTime fromDate, DateTime toDate, Guid? tenantId = null)
        {
            var dbSet = await GetDbSetAsync();
            var query = dbSet
                .Where(t =>
                    t.Status == "succeeded" &&
                    t.CompletedAt >= fromDate &&
                    t.CompletedAt <= toDate);

            if (tenantId.HasValue)
            {
                query = query.Where(t => t.TenantId == tenantId);
            }

            var totalAmount = await query.SumAsync(t => t.Amount);
            return totalAmount;
        }

        public async Task<int> GetFailedTransactionsCountAsync(DateTime fromDate, DateTime toDate)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .CountAsync(t =>
                    (t.Status == "canceled" || t.Status == "requires_payment_method") &&
                    t.CreationTime >= fromDate &&
                    t.CreationTime <= toDate);
        }

        public async Task<List<StripeTransaction>> GetCompletedTransactionsAsync(DateTime fromDate, DateTime toDate)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(t =>
                    t.Status == "succeeded" &&
                    t.CompletedAt >= fromDate &&
                    t.CompletedAt <= toDate)
                .OrderByDescending(t => t.CompletedAt)
                .ToListAsync();
        }
    }
}