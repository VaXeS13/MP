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

                var totalSheetsUnassigned = 0;

                foreach (var rental in expiredRentals)
                {
                    try
                    {
                        var sheetsUnassigned = await ProcessExpiredRentalAsync(rental, itemSheetRepository);
                        totalSheetsUnassigned += sheetsUnassigned;

                        _logger.LogInformation("ExpiredRentalItemCleanupWorker: Processed expired rental {RentalId} ({BoothId}), unassigned {SheetsCount} item sheets",
                            rental.Id, rental.BoothId, sheetsUnassigned);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ExpiredRentalItemCleanupWorker: Error processing expired rental {RentalId}", rental.Id);
                        // Continue with next rental even if one fails
                    }
                }

                _logger.LogInformation("ExpiredRentalItemCleanupWorker: Completed cleanup, unassigned {TotalSheets} item sheets from {RentalCount} rentals",
                    totalSheetsUnassigned, expiredRentals.Count);
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

            var sheetsUnassigned = 0;

            foreach (var sheet in assignedSheets)
            {
                try
                {
                    // Only unassign sheets that are in Assigned status
                    // Sheets in Ready status cannot be unassigned (they have generated barcodes)
                    if (sheet.Status == ItemSheetStatus.Assigned)
                    {
                        sheet.UnassignFromRental();
                        await itemSheetRepository.UpdateAsync(sheet);
                        sheetsUnassigned++;

                        _logger.LogDebug("ExpiredRentalItemCleanupWorker: Unassigned item sheet {SheetId} from expired rental {RentalId}",
                            sheet.Id, rental.Id);
                    }
                    else if (sheet.Status == ItemSheetStatus.Ready)
                    {
                        _logger.LogWarning("ExpiredRentalItemCleanupWorker: Cannot unassign Ready item sheet {SheetId} from expired rental {RentalId}. Sheet must be in Assigned status.",
                            sheet.Id, rental.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ExpiredRentalItemCleanupWorker: Error unassigning item sheet {SheetId} from rental {RentalId}",
                        sheet.Id, rental.Id);
                    // Continue with next sheet even if one fails
                }
            }

            return sheetsUnassigned;
        }
    }
}
