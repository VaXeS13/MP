using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Uow;
using Volo.Abp.Timing;
using MP.Application.Contracts.SignalR;
using MP.Domain.Rentals;
using MP.Domain.Items;
using MP.Domain.Notifications;
using MP.Rentals;
using MP.Items;
using MP.Application.Contracts.Notifications;

namespace MP.Application.Notifications
{
    /// <summary>
    /// Background worker that sends reminder notifications for expiring rentals and items
    /// Runs daily to check for upcoming expirations and sends reminder notifications
    /// </summary>
    public class NotificationReminderWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<NotificationReminderWorker> _logger;
        private readonly IClock _clock;
        private readonly TimeSpan _period = TimeSpan.FromDays(1); // Run daily

        public NotificationReminderWorker(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<NotificationReminderWorker> logger,
            IClock clock)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _clock = clock;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Calculate delay until next 2:00 AM to run during off-peak hours
            var now = _clock.Now;
            var nextRun = new DateTime(now.Year, now.Month, now.Day, 2, 0, 0);
            if (nextRun <= now)
            {
                nextRun = nextRun.AddDays(1);
            }

            var initialDelay = nextRun - now;
            _logger.LogInformation("NotificationReminderWorker: Will start at {NextRun:yyyy-MM-dd HH:mm:ss} (in {InitialDelay:hh\\:mm\\:ss})",
                nextRun, initialDelay);

            await Task.Delay(initialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("NotificationReminderWorker: Starting daily reminder check");
                    await DoWorkAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "NotificationReminderWorker: Error during reminder execution");
                }

                // Wait for the next period
                await Task.Delay(_period, stoppingToken);
            }
        }

        [UnitOfWork]
        private async Task DoWorkAsync()
        {
            using var scope = _serviceScopeFactory.CreateScope();

            var rentalRepository = scope.ServiceProvider.GetRequiredService<IRentalRepository>();
            var itemRepository = scope.ServiceProvider.GetRequiredService<IItemRepository>();
            var notificationAppService = scope.ServiceProvider.GetRequiredService<INotificationAppService>();

            var now = _clock.Now;
            var totalNotificationsSent = 0;

            try
            {
                // Check for rentals expiring in 3 days
                var rentalNotificationsSent = await CheckExpiringRentalsAsync(rentalRepository, notificationAppService, now);
                totalNotificationsSent += rentalNotificationsSent;

                // Check for items expiring in 7 days
                var itemNotificationsSent = await CheckExpiringItemsAsync(itemRepository, notificationAppService, now);
                totalNotificationsSent += itemNotificationsSent;

                _logger.LogInformation("NotificationReminderWorker: Completed daily check. Sent {TotalCount} notifications ({RentalCount} rentals, {ItemCount} items)",
                    totalNotificationsSent, rentalNotificationsSent, itemNotificationsSent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationReminderWorker: Error during daily reminder check");
            }
        }

        private async Task<int> CheckExpiringRentalsAsync(
            IRentalRepository rentalRepository,
            INotificationAppService notificationAppService,
            DateTime now)
        {
            var notificationCount = 0;
            var threeDaysFromNow = now.AddDays(3);
            var oneDayFromNow = now.AddDays(1);

            try
            {
                // Get active rentals expiring in the next 3 days
                var expiringRentals = await GetExpiringRentalsAsync(rentalRepository, threeDaysFromNow);

                _logger.LogInformation("NotificationReminderWorker: Found {Count} rentals expiring in next 3 days", expiringRentals.Count);

                foreach (var rental in expiringRentals)
                {
                    try
                    {
                        var daysUntilExpiry = (rental.Period.EndDate.Date - now.Date).Days;
                        var isUrgent = daysUntilExpiry <= 1;

                        var notification = new NotificationMessageDto
                        {
                            Id = Guid.NewGuid(),
                            Type = isUrgent ? NotificationTypes.RentalExpiring : NotificationTypes.RentalExpiring,
                            Title = isUrgent ? "Wynajem wygasa jutro!" : "Wynajem wygasa wkrótce",
                            Message = $"Twój wynajem stanowiska {rental.Booth.Number} wygasa {rental.Period.EndDate:dd.MM.yyyy} ({daysUntilExpiry} dni). " +
                                     $"Możesz przedłużyć wynajem w panelu.",
                            Severity = isUrgent ? "warning" : "info",
                            ActionUrl = $"/rentals/{rental.Id}/extend",
                            CreatedAt = now
                        };

                        await notificationAppService.SendToUserAsync(rental.UserId, notification);
                        notificationCount++;

                        _logger.LogDebug("NotificationReminderWorker: Sent rental expiry reminder for rental {RentalId} to user {UserId}",
                            rental.Id, rental.UserId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "NotificationReminderWorker: Failed to send rental expiry reminder for rental {RentalId}", rental.Id);
                    }
                }

                return notificationCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationReminderWorker: Error checking expiring rentals");
                return 0;
            }
        }

        private async Task<int> CheckExpiringItemsAsync(
            IItemRepository itemRepository,
            INotificationAppService notificationAppService,
            DateTime now)
        {
            // Item expiry reminders are not applicable as Item entity doesn't have expiry dates
            // This functionality can be implemented later when Item expires at field is added
            _logger.LogDebug("NotificationReminderWorker: Item expiry checking skipped - Item entity doesn't have expiry dates");
            return 0;
        }

        private async Task<List<Rental>> GetExpiringRentalsAsync(IRentalRepository rentalRepository, DateTime endDate)
        {
            var queryable = await rentalRepository.GetQueryableAsync();
            var now = _clock.Now;

            var expiringRentals = queryable
                .Where(r => r.Status == RentalStatus.Active &&
                           r.Period.EndDate <= endDate &&
                           r.Period.EndDate > now)
                .ToList();

            return expiringRentals;
        }

        private async Task<List<Item>> GetExpiringItemsAsync(IItemRepository itemRepository, DateTime expiryDate)
        {
            // Item entity doesn't have expiry dates, so return empty list
            // This functionality can be implemented later when Item expires at field is added
            return new List<Item>();
        }
    }
}