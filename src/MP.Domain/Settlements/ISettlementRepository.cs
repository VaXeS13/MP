using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.Settlements
{
    /// <summary>
    /// Settlement repository
    /// </summary>
    public interface ISettlementRepository : IRepository<Settlement, Guid>
    {
        Task<List<Settlement>> GetUserSettlementsAsync(
            Guid userId,
            SettlementStatus? status = null,
            DateTime? createdAfter = null,
            DateTime? createdBefore = null,
            int skipCount = 0,
            int maxResultCount = 10,
            string? sorting = null,
            CancellationToken cancellationToken = default);

        Task<int> GetUserSettlementsCountAsync(
            Guid userId,
            SettlementStatus? status = null,
            DateTime? createdAfter = null,
            DateTime? createdBefore = null,
            CancellationToken cancellationToken = default);

        Task<Settlement?> FindBySettlementNumberAsync(
            string settlementNumber,
            CancellationToken cancellationToken = default);

        Task<List<Settlement>> GetPendingSettlementsAsync(
            CancellationToken cancellationToken = default);

        Task<decimal> GetUserTotalEarningsAsync(
            Guid userId,
            SettlementStatus? status = null,
            CancellationToken cancellationToken = default);
    }
}
