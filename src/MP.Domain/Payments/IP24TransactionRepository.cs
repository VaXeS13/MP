using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.Payments
{
    public interface IP24TransactionRepository : ITransactionRepository<P24Transaction>
    {
        Task<P24Transaction?> FindBySessionIdAsync(string sessionId);

        Task<List<P24Transaction>> GetTransactionsForStatusCheckAsync(int maxCheckCount = 3);

        Task<List<P24Transaction>> GetUnverifiedTransactionsAsync();
    }
}