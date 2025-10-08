using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MP.Domain.Payments;
using MP.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace MP.EntityFrameworkCore.Payments
{
    public class EfCoreP24TransactionRepository : EfCoreRepository<MPDbContext, P24Transaction, Guid>, IP24TransactionRepository
    {
        public EfCoreP24TransactionRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<P24Transaction?> FindBySessionIdAsync(string sessionId)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.P24Transactions
                .Where(t => t.SessionId == sessionId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<P24Transaction>> GetTransactionsForStatusCheckAsync(int maxCheckCount = 3)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(t => t.ManualStatusCheckCount < maxCheckCount &&
                           t.Verified == false)
                .OrderBy(t => t.CreationTime)
                .ToListAsync();
        }

        public async Task<List<P24Transaction>> GetByRentalIdAsync(Guid rentalId)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.P24Transactions
                .Where(t => t.RentalId == rentalId)
                .OrderByDescending(t => t.CreationTime)
                .ToListAsync();
        }

        public async Task<List<P24Transaction>> GetByEmailAsync(string email)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.P24Transactions
                .Where(t => t.Email == email)
                .OrderByDescending(t => t.CreationTime)
                .ToListAsync();
        }

        public async Task<List<P24Transaction>> GetByStatusAsync(string status)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.P24Transactions
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.CreationTime)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalAmountAsync(DateTime fromDate, DateTime toDate, Guid? tenantId = null)
        {
            var dbContext = await GetDbContextAsync();
            var query = dbContext.P24Transactions
                .Where(t => t.Status == "completed" &&
                           t.CreationTime >= fromDate &&
                           t.CreationTime <= toDate);

            if (tenantId.HasValue)
            {
                query = query.Where(t => t.TenantId == tenantId);
            }

            var totalAmount = await query.SumAsync(t => (decimal)t.Amount);
            return totalAmount / 100; // P24 stores amount in grosze (cents)
        }

        public async Task<List<P24Transaction>> GetPendingStatusChecksAsync(DateTime olderThan, int maxCount = 100)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.P24Transactions
                .Where(t => t.Status == "processing" &&
                           (t.LastStatusCheck == null || t.LastStatusCheck < olderThan) &&
                           t.ManualStatusCheckCount < 10)
                .OrderBy(t => t.LastStatusCheck ?? t.CreationTime)
                .Take(maxCount)
                .ToListAsync();
        }

        public async Task<List<P24Transaction>> GetCompletedTransactionsAsync(DateTime fromDate, DateTime toDate)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.P24Transactions
                .Where(t => t.Status == "completed" &&
                           t.CreationTime >= fromDate &&
                           t.CreationTime <= toDate)
                .OrderByDescending(t => t.CreationTime)
                .ToListAsync();
        }

        public async Task<int> GetFailedTransactionsCountAsync(DateTime fromDate, DateTime toDate)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.P24Transactions
                .CountAsync(t => (t.Status == "failed" || t.Status == "cancelled") &&
                               t.CreationTime >= fromDate &&
                               t.CreationTime <= toDate);
        }

        public async Task<List<P24Transaction>> GetUnverifiedTransactionsAsync()
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.P24Transactions
                .Where(t => !t.Verified)
                .OrderBy(t => t.CreationTime)
                .ToListAsync();
        }
    }
}