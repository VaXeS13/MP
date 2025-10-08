using Microsoft.EntityFrameworkCore;
using MP.Domain.Carts;
using MP.EntityFrameworkCore;
using MP.Carts;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace MP.EntityFrameworkCore.Carts
{
    public class EfCoreCartRepository : EfCoreRepository<MPDbContext, Cart, Guid>, ICartRepository
    {
        public EfCoreCartRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<Cart?> GetActiveCartByUserIdAsync(
            Guid userId,
            bool includeItems = false,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var query = dbContext.Carts
                .Where(c => c.UserId == userId && c.Status == CartStatus.Active);

            if (includeItems)
            {
                query = query.Include(c => c.Items);
            }

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Cart?> GetCartWithItemsAsync(
            Guid cartId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Carts
                .Include(c => c.Items)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == cartId, cancellationToken);
        }

        public async Task<bool> HasActiveCartItemForBoothAsync(
            Guid boothId,
            DateTime startDate,
            DateTime endDate,
            Guid? excludeCartId = null,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();

            var query = dbContext.CartItems
                .Include(ci => ci.Cart)
                .Where(ci =>
                    ci.BoothId == boothId &&
                    ci.Cart.Status == CartStatus.Active &&
                    ci.StartDate <= endDate &&
                    ci.EndDate >= startDate);

            if (excludeCartId.HasValue)
            {
                query = query.Where(ci => ci.CartId != excludeCartId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        }
    }
}