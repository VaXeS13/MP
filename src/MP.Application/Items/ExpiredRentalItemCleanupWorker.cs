using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Uow;
using MP.Domain.Items;
using MP.Domain.Rentals;

namespace MP.Items
{
    /// <summary>
    /// Background worker that frees items from expired rentals for reassignment.
    /// Runs periodically (every 5 minutes) to check for rentals with EndDate in the past.
    /// For each expired rental, unassigns all ItemSheets from the rental,
    /// allowing the items to be reassigned to new sheets.
    /// </summary>
    public class ExpiredRentalItemCleanupWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<ExpiredRentalItemCleanupWorker> _logger;
        private readonly TimeSpan _period = TimeSpan.FromMinutes(5); // Run every 5 minutes

        public ExpiredRentalItemCleanupWorker(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ExpiredRentalItemCleanupWorker> logger)
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
                    _logger.LogError(ex, "ExpiredRentalItemCleanupWorker: Error during cleanup execution");
                }

                // Wait for the next period
                await Task.Delay(_period, stoppingToken);
            }
        }

        [UnitOfWork]
        private async Task DoWorkAsync()
        {
            using var scope = _serviceScopeFactory.CreateScope();

            _logger.LogInformation("ExpiredRentalItemCleanupWorker: Starting cleanup of expired rental item assignments");

            var rentalRepository = scope.ServiceProvider.GetRequiredService<IRentalRepository>();
            var itemSheetRepository = scope.ServiceProvider.GetRequiredService<IItemSheetRepository>();

            try
            {
                // Get all rentals that have expired (EndDate before today)
                var expiredRentals = await GetExpiredRentalsAsync(rentalRepository);

                if (expiredRentals.Count == 0)
                {
                    _logger.LogDebug("ExpiredRentalItemCleanupWorker: No expired rentals found");
                    return;
                }

                _logger.LogInformation("ExpiredRentalItemCleanupWorker: Found {ExpiredRentalCount} expired rentals to process", expiredRentals.Count);

                var totalSheetsProcessed = 0;

                foreach (var rental in expiredRentals)
                {
                    try
                    {
                        var sheetsProcessed = await ProcessExpiredRentalAsync(rental, itemSheetRepository);
                        totalSheetsProcessed += sheetsProcessed;

                        _logger.LogInformation("ExpiredRentalItemCleanupWorker: Processed expired rental {RentalId} ({BoothId}), processed {SheetsCount} item sheets",
                            rental.Id, rental.BoothId, sheetsProcessed);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ExpiredRentalItemCleanupWorker: Error processing expired rental {RentalId}", rental.Id);
                        // Continue with next rental even if one fails
                    }
                }

                _logger.LogInformation("ExpiredRentalItemCleanupWorker: Completed cleanup, processed {TotalSheets} item sheets from {RentalCount} rentals",
                    totalSheetsProcessed, expiredRentals.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExpiredRentalItemCleanupWorker: Error during item sheet cleanup");
            }
        }

        private async Task<List<Rental>> GetExpiredRentalsAsync(IRentalRepository rentalRepository)
        {
            // Get rentals that have expired (EndDate is before today)
            // This includes rentals with Expired status and those with Active/Extended status but past EndDate
            return await rentalRepository.GetExpiredRentalsAsync(DateTime.Today);
        }

        private async Task<int> ProcessExpiredRentalAsync(
            Rental rental,
            IItemSheetRepository itemSheetRepository)
        {
            // Get all ItemSheets assigned to this rental
            var assignedSheets = await itemSheetRepository.GetListByRentalIdAsync(rental.Id);

            if (assignedSheets.Count == 0)
            {
                _logger.LogDebug("ExpiredRentalItemCleanupWorker: Rental {RentalId} has no assigned item sheets", rental.Id);
                return 0;
            }

            var sheetsProcessed = 0;

            foreach (var sheet in assignedSheets)
            {
                try
                {
                    if (sheet.Status == ItemSheetStatus.Assigned)
                    {
                        // Unassign sheets that are in Assigned status
                        sheet.UnassignFromRental();
                        await itemSheetRepository.UpdateAsync(sheet);
                        sheetsProcessed++;

                        _logger.LogDebug("ExpiredRentalItemCleanupWorker: Unassigned item sheet {SheetId} from expired rental {RentalId}",
                            sheet.Id, rental.Id);
                    }
                    else if (sheet.Status == ItemSheetStatus.Ready)
                    {
                        // Process Ready sheets: return unsold items to Available and mark sheet as Completed
                        ProcessReadySheet(sheet);
                        await itemSheetRepository.UpdateAsync(sheet);
                        sheetsProcessed++;

                        _logger.LogInformation("ExpiredRentalItemCleanupWorker: Completed item sheet {SheetId} from expired rental {RentalId}. Unsold items returned to available.",
                            sheet.Id, rental.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ExpiredRentalItemCleanupWorker: Error processing item sheet {SheetId} from rental {RentalId}",
                        sheet.Id, rental.Id);
                    // Continue with next sheet even if one fails
                }
            }

            return sheetsProcessed;
        }

        private void ProcessReadySheet(ItemSheet sheet)
        {
            var soldItemsCount = 0;
            var unsoldItemsCount = 0;

            // Return unsold items to Available and mark sheet as Completed
            foreach (var sheetItem in sheet.Items)
            {
                if (sheetItem.Item != null)
                {
                    if (sheetItem.Item.Status == ItemStatus.Sold)
                    {
                        soldItemsCount++;
                    }
                    else
                    {
                        // Return unsold items to available
                        sheetItem.Item.MarkAsAvailable();
                        unsoldItemsCount++;

                        _logger.LogDebug("ExpiredRentalItemCleanupWorker: Returned item {ItemId} to available status",
                            sheetItem.Item.Id);
                    }
                }
            }

            // Mark sheet as completed after processing items
            sheet.MarkAsCompleted();

            if (unsoldItemsCount > 0 || soldItemsCount > 0)
            {
                _logger.LogDebug("ExpiredRentalItemCleanupWorker: Processed sheet items - {SoldCount} sold, {UnsoldCount} returned to available",
                    soldItemsCount, unsoldItemsCount);
            }
        }
    }
}
