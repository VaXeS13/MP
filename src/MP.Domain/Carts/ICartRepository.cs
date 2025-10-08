using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using MP.Carts;

namespace MP.Domain.Carts
{
    public interface ICartRepository : IRepository<Cart, Guid>
    {
        /// <summary>
        /// Gets the active cart for a user, or null if not found
        /// </summary>
        Task<Cart?> GetActiveCartByUserIdAsync(
            Guid userId,
            bool includeItems = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a cart with all its items
        /// </summary>
        Task<Cart?> GetCartWithItemsAsync(
            Guid cartId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a booth has any active cart items in a given period
        /// </summary>
        Task<bool> HasActiveCartItemForBoothAsync(
            Guid boothId,
            DateTime startDate,
            DateTime endDate,
            Guid? excludeCartId = null,
            CancellationToken cancellationToken = default);
    }
}