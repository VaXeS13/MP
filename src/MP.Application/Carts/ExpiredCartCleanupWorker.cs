using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Uow;
using MP.Domain.Carts;
using MP.Domain.Rentals;
using MP.Rentals;

namespace MP.Carts
{
    /// <summary>
    /// Background worker that releases expired cart item reservations
    /// Runs periodically to check CartItems with ReservationExpiresAt in the past
    /// Releases booth blocking by removing RentalId, but KEEPS CartItem in cart
    /// Deletes associated Draft Rentals to free up the booth for other users
    /// </summary>
    public class ExpiredCartCleanupWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<ExpiredCartCleanupWorker> _logger;
        private readonly TimeSpan _period = TimeSpan.FromMinutes(5); // Run every 5 minutes

        public ExpiredCartCleanupWorker(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ExpiredCartCleanupWorker> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DoWorkAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ExpiredCartCleanupWorker: Error during cart cleanup execution");
                }

                // Wait for the next period
                await Task.Delay(_period, stoppingToken);
            }
        }

        [UnitOfWork]
        private async Task DoWorkAsync()
        {
            using var scope = _serviceScopeFactory.CreateScope();

            _logger.LogInformation("ExpiredCartCleanupWorker: Starting cleanup of expired cart item reservations");

            var cartRepository = scope.ServiceProvider.GetRequiredService<ICartRepository>();
            var rentalRepository = scope.ServiceProvider.GetRequiredService<IRentalRepository>();

            try
            {
                // Get all cart items with expired reservations
                var expiredItems = await GetExpiredCartItemsAsync(cartRepository);

                if (expiredItems.Count == 0)
                {
                    _logger.LogDebug("ExpiredCartCleanupWorker: No expired cart items found");
                    return;
                }

                _logger.LogInformation("ExpiredCartCleanupWorker: Found {ExpiredItemCount} expired cart items to process", expiredItems.Count);

                foreach (var item in expiredItems)
                {
                    try
                    {
                        await ProcessExpiredCartItemAsync(item, rentalRepository, cartRepository);
                        _logger.LogInformation("ExpiredCartCleanupWorker: Processed expired cart item {CartItemId} from cart {CartId}",
                            item.Id, item.CartId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ExpiredCartCleanupWorker: Error processing expired cart item {CartItemId}", item.Id);
                        // Continue with next item even if one fails
                    }
                }

                _logger.LogInformation("ExpiredCartCleanupWorker: Completed cleanup of {ExpiredItemCount} expired cart items",
                    expiredItems.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExpiredCartCleanupWorker: Error during cart item cleanup");
            }
        }

        private async Task<System.Collections.Generic.List<CartItem>> GetExpiredCartItemsAsync(ICartRepository cartRepository)
        {
            var queryable = await cartRepository.GetQueryableAsync();
            var now = DateTime.Now;

            // Find all cart items with expired reservations
            var expiredItems = queryable
                .Where(c => c.Status == CartStatus.Active)
                .SelectMany(c => c.Items)
                .Where(item => item.ReservationExpiresAt.HasValue &&
                              item.ReservationExpiresAt.Value < now)
                .ToList();

            return expiredItems;
        }

        private async Task ProcessExpiredCartItemAsync(
            CartItem item,
            IRentalRepository rentalRepository,
            ICartRepository cartRepository)
        {
            // If item has linked Rental (admin-created with online payment), soft delete it
            if (item.RentalId.HasValue)
            {
                try
                {
                    var rental = await rentalRepository.GetAsync(item.RentalId.Value);

                    // Only delete if still in Draft status (not yet paid)
                    if (rental.Status == RentalStatus.Draft)
                    {
                        await rentalRepository.DeleteAsync(rental);
                        _logger.LogDebug("ExpiredCartCleanupWorker: Soft deleted Draft Rental {RentalId} for expired cart item {CartItemId}",
                            rental.Id, item.Id);
                    }
                    else
                    {
                        _logger.LogWarning("ExpiredCartCleanupWorker: Rental {RentalId} has status {Status}, skipping deletion",
                            rental.Id, rental.Status);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ExpiredCartCleanupWorker: Error deleting rental {RentalId} for cart item {CartItemId}",
                        item.RentalId, item.Id);
                }
            }

            // Release reservation (remove RentalId) but KEEP the CartItem in cart
            // This allows user to see expired items and manually remove or update them
            // Booth becomes available for other users since ReservationExpiresAt is in the past
            var cart = await cartRepository.GetCartWithItemsAsync(item.CartId);
            if (cart != null)
            {
                var cartItem = cart.Items.FirstOrDefault(i => i.Id == item.Id);
                if (cartItem != null)
                {
                    cartItem.ReleaseReservation(); // Remove RentalId, keep ReservationExpiresAt for history
                    await cartRepository.UpdateAsync(cart);

                    _logger.LogInformation("ExpiredCartCleanupWorker: Released reservation for expired cart item {CartItemId} in cart {CartId} (item remains in cart)",
                        item.Id, item.CartId);
                }
            }
        }
    }
}
