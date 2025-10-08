using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.Items
{
    public interface IItemRepository : IRepository<Item, Guid>
    {
        Task<List<Item>> GetListByUserIdAsync(
            Guid userId,
            int skipCount,
            int maxResultCount,
            ItemStatus? status = null,
            CancellationToken cancellationToken = default);

        Task<List<Item>> GetListByIdsAsync(
            List<Guid> ids,
            CancellationToken cancellationToken = default);

        Task<int> GetCountByUserIdAsync(
            Guid userId,
            ItemStatus? status = null,
            CancellationToken cancellationToken = default);
    }
}
