using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.Payments
{
    public interface ITransactionRepository<TTransaction> : IRepository<TTransaction, Guid>
        where TTransaction : class, IEntity<Guid>
    {
        Task<List<TTransaction>> GetByRentalIdAsync(Guid rentalId);

        Task<List<TTransaction>> GetByEmailAsync(string email);

        Task<List<TTransaction>> GetByStatusAsync(string status);

        Task<decimal> GetTotalAmountAsync(DateTime fromDate, DateTime toDate, Guid? tenantId = null);

        Task<List<TTransaction>> GetPendingStatusChecksAsync(DateTime olderThan, int maxCount = 100);

        Task<List<TTransaction>> GetCompletedTransactionsAsync(DateTime fromDate, DateTime toDate);

        Task<int> GetFailedTransactionsCountAsync(DateTime fromDate, DateTime toDate);
    }
}