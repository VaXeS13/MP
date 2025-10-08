using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MP.Domain.Items;
using MP.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace MP.EntityFrameworkCore.Items
{
    public class EfCoreItemSheetRepository : EfCoreRepository<MPDbContext, ItemSheet, Guid>, IItemSheetRepository
    {
        public EfCoreItemSheetRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<List<ItemSheet>> GetListByUserIdAsync(
            Guid userId,
            ItemSheetStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            var query = dbSet
                .Include(x => x.Items)
                .Include(x => x.Rental)
                    .ThenInclude(x => x.Booth)
                .Where(x => x.UserId == userId);

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            return await query
                .OrderByDescending(x => x.CreationTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ItemSheet>> GetListByRentalIdAsync(
            Guid rentalId,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Include(x => x.Items)
                .Where(x => x.RentalId == rentalId)
                .OrderByDescending(x => x.CreationTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<ItemSheet?> FindByBarcodeAsync(
            string barcode,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Include(x => x.Items)
                .ThenInclude(x => x.Item)
                .FirstOrDefaultAsync(
                    x => x.Items.Any(i => i.Barcode == barcode),
                    cancellationToken);
        }

        public async Task<ItemSheet?> GetWithItemsAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Include(x => x.Items)
                    .ThenInclude(x => x.Item)
                .Include(x => x.Rental)
                    .ThenInclude(x => x.Booth)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }
    }
}
