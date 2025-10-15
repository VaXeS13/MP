using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Users;
using MP.Application.Contracts.SignalR;
using MP.Domain.Rentals.Events;
using MP.Domain.Notifications;
using MP.Domain.Rentals;
using MP.Domain.Items;
using MP.Domain.Payments;
using MP.Domain.Settlements;
using Volo.Abp.EventBus;
using MP.Application.Contracts.Notifications;

namespace MP.Application.Notifications
{
    /// <summary>
    /// Handles domain events and creates notifications
    /// </summary>
    public class NotificationEventHandler :
        ILocalEventHandler<RentalConfirmedEvent>,
        ILocalEventHandler<RentalCompletedEvent>,
        ILocalEventHandler<RentalCancelledEvent>,
        ILocalEventHandler<RentalExtendedEvent>,
        IDistributedEventHandler<ItemSoldEvent>,
        IDistributedEventHandler<PaymentCompletedEvent>,
        IDistributedEventHandler<PaymentFailedEvent>,
        IDistributedEventHandler<SettlementReadyEvent>,
        IDistributedEventHandler<SettlementPaidEvent>,
        ITransientDependency
    {
        private readonly INotificationAppService _notificationAppService;
        private readonly ILogger<NotificationEventHandler> _logger;
        private readonly ICurrentUser _currentUser;

        public NotificationEventHandler(
            INotificationAppService notificationAppService,
            ILogger<NotificationEventHandler> logger,
            ICurrentUser currentUser)
        {
            _notificationAppService = notificationAppService;
            _logger = logger;
            _currentUser = currentUser;
        }

        public async Task HandleEventAsync(RentalConfirmedEvent eventData)
        {
            var rental = eventData.Entity;

            try
            {
                var notification = new NotificationMessageDto
                {
                    Id = Guid.NewGuid(),
                    Type = NotificationTypes.RentalStarted,
                    Title = "Wynajem rozpoczęty",
                    Message = $"Twój wynajem stanowiska {rental.Booth.Number} został rozpoczęty. Okres wynajmu: {rental.Period.StartDate:dd.MM.yyyy} - {rental.Period.EndDate:dd.MM.yyyy}",
                    Severity = "success",
                    ActionUrl = $"/rentals/{rental.Id}",
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationAppService.SendToUserAsync(rental.UserId, notification);
                _logger.LogInformation("Created rental started notification for user {UserId}, rental {RentalId}", rental.UserId, rental.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create rental started notification for rental {RentalId}", rental.Id);
            }
        }

        public async Task HandleEventAsync(RentalCompletedEvent eventData)
        {
            var rental = eventData.Entity;

            try
            {
                var notification = new NotificationMessageDto
                {
                    Id = Guid.NewGuid(),
                    Type = NotificationTypes.RentalCompleted,
                    Title = "Wynajem zakończony",
                    Message = $"Twój wynajem stanowiska {rental.Booth.Number} został zakończony. Dziękujemy za korzystanie z naszych usług!",
                    Severity = "info",
                    ActionUrl = $"/rentals/{rental.Id}",
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationAppService.SendToUserAsync(rental.UserId, notification);
                _logger.LogInformation("Created rental completed notification for user {UserId}, rental {RentalId}", rental.UserId, rental.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create rental completed notification for rental {RentalId}", rental.Id);
            }
        }

        public async Task HandleEventAsync(RentalCancelledEvent eventData)
        {
            var rental = eventData.Entity;

            try
            {
                var notification = new NotificationMessageDto
                {
                    Id = Guid.NewGuid(),
                    Type = NotificationTypes.RentalCompleted,
                    Title = "Wynajem anulowany",
                    Message = $"Twój wynajem stanowiska {rental.Booth.Number} został anulowany.",
                    Severity = "warning",
                    ActionUrl = $"/rentals/{rental.Id}",
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationAppService.SendToUserAsync(rental.UserId, notification);
                _logger.LogInformation("Created rental cancelled notification for user {UserId}, rental {RentalId}", rental.UserId, rental.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create rental cancelled notification for rental {RentalId}", rental.Id);
            }
        }

        public async Task HandleEventAsync(RentalExtendedEvent eventData)
        {
            var rental = eventData.Entity;

            try
            {
                var notification = new NotificationMessageDto
                {
                    Id = Guid.NewGuid(),
                    Type = NotificationTypes.RentalExtended,
                    Title = "Wynajem przedłużony",
                    Message = $"Twój wynajem stanowiska {rental.Booth.Number} został przedłużony do {rental.Period.EndDate:dd.MM.yyyy}",
                    Severity = "success",
                    ActionUrl = $"/rentals/{rental.Id}",
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationAppService.SendToUserAsync(rental.UserId, notification);
                _logger.LogInformation("Created rental extended notification for user {UserId}, rental {RentalId}", rental.UserId, rental.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create rental extended notification for rental {RentalId}", rental.Id);
            }
        }

        public async Task HandleEventAsync(ItemSoldEvent eventData)
        {
            // This will be implemented when ItemSoldEvent is created
            // For now, this is a placeholder
            _logger.LogInformation("ItemSoldEvent received but not yet implemented for notifications");
        }

        public async Task HandleEventAsync(PaymentCompletedEvent eventData)
        {
            // This will be implemented when PaymentCompletedEvent is created
            // For now, this is a placeholder
            _logger.LogInformation("PaymentCompletedEvent received but not yet implemented for notifications");
        }

        public async Task HandleEventAsync(PaymentFailedEvent eventData)
        {
            // This will be implemented when PaymentFailedEvent is created
            // For now, this is a placeholder
            _logger.LogInformation("PaymentFailedEvent received but not yet implemented for notifications");
        }

        public async Task HandleEventAsync(SettlementReadyEvent eventData)
        {
            // This will be implemented when SettlementReadyEvent is created
            // For now, this is a placeholder
            _logger.LogInformation("SettlementReadyEvent received but not yet implemented for notifications");
        }

        public async Task HandleEventAsync(SettlementPaidEvent eventData)
        {
            // This will be implemented when SettlementPaidEvent is created
            // For now, this is a placeholder
            _logger.LogInformation("SettlementPaidEvent received but not yet implemented for notifications");
        }
    }

    // Placeholder events that need to be created in the respective domain modules
    public class ItemSoldEvent
    {
        public Guid ItemId { get; set; }
        public Guid UserId { get; set; }
        public string ItemName { get; set; }
        public decimal Price { get; set; }
    }

    public class PaymentCompletedEvent
    {
        public Guid PaymentId { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
    }

    public class PaymentFailedEvent
    {
        public Guid PaymentId { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; }
    }

    public class SettlementReadyEvent
    {
        public Guid SettlementId { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
    }

    public class SettlementPaidEvent
    {
        public Guid SettlementId { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
    }
}