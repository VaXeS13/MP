using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.Items
{
    public interface IItemSheetRepository : IRepository<ItemSheet, Guid>
    {
        Task<List<ItemSheet>> GetListByUserIdAsync(
            Guid userId,
            ItemSheetStatus? status = null,
            CancellationToken cancellationToken = default);

        Task<List<ItemSheet>> GetListByRentalIdAsync(
            Guid rentalId,
            CancellationToken cancellationToken = default);

        Task<ItemSheet?> FindByBarcodeAsync(
            string barcode,
            CancellationToken cancellationToken = default);

        Task<ItemSheet?> GetWithItemsAsync(
            Guid id,
            CancellationToken cancellationToken = default);
    }
}
