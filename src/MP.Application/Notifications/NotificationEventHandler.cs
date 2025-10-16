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
using MP.Domain.Items.Events;
using MP.Domain.Payments;
using MP.Domain.Payments.Events;
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
        ILocalEventHandler<PaymentInitiatedEvent>,
        ILocalEventHandler<PaymentCompletedEvent>,
        ILocalEventHandler<PaymentFailedEvent>,
        ILocalEventHandler<ItemSoldEvent>,
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

        public async Task HandleEventAsync(PaymentInitiatedEvent eventData)
        {
            try
            {
                var notification = new NotificationMessageDto
                {
                    Id = Guid.NewGuid(),
                    Type = NotificationTypes.PaymentReceived, // Using existing type temporarily
                    Title = "Rozpoczęto proces płatności",
                    Message = $"Rozpoczęto proces płatności za wynajem {eventData.RentalIds.Count} stanowisk o wartości {eventData.Amount:F2} {eventData.Currency}. Numer transakcji: {eventData.TransactionId}",
                    Severity = "info",
                    ActionUrl = $"/rentals/my-rentals",
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationAppService.SendToUserAsync(eventData.UserId, notification);
                _logger.LogInformation("Created payment initiated notification for user {UserId}, transaction {TransactionId}",
                    eventData.UserId, eventData.TransactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create payment initiated notification for transaction {TransactionId}",
                    eventData.TransactionId);
            }
        }

        public async Task HandleEventAsync(PaymentCompletedEvent eventData)
        {
            try
            {
                var notification = new NotificationMessageDto
                {
                    Id = Guid.NewGuid(),
                    Type = NotificationTypes.PaymentReceived,
                    Title = "Płatność zakończona sukcesem!",
                    Message = $"✅ Płatność za wynajem {eventData.RentalIds.Count} stanowisk o wartości {eventData.Amount:F2} {eventData.Currency} została potwierdzona. Twoje stanowiska są teraz aktywne. Numer transakcji: {eventData.TransactionId}",
                    Severity = "success",
                    ActionUrl = $"/rentals/my-rentals",
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationAppService.SendToUserAsync(eventData.UserId, notification);
                _logger.LogInformation("Created payment completed notification for user {UserId}, transaction {TransactionId}",
                    eventData.UserId, eventData.TransactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create payment completed notification for transaction {TransactionId}",
                    eventData.TransactionId);
            }
        }

        public async Task HandleEventAsync(PaymentFailedEvent eventData)
        {
            try
            {
                var notification = new NotificationMessageDto
                {
                    Id = Guid.NewGuid(),
                    Type = NotificationTypes.PaymentFailed,
                    Title = "Płatność nie powiodła się",
                    Message = $"❌ Płatność za wynajem stanowisk nie została ukończona. Powód: {eventData.Reason}. Stanowiska zostały zwolnione. Możesz spróbować ponownie.",
                    Severity = "error",
                    ActionUrl = $"/booths",
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationAppService.SendToUserAsync(eventData.UserId, notification);
                _logger.LogInformation("Created payment failed notification for user {UserId}, transaction {TransactionId}",
                    eventData.UserId, eventData.TransactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create payment failed notification for transaction {TransactionId}",
                    eventData.TransactionId);
            }
        }

        public async Task HandleEventAsync(ItemSoldEvent eventData)
        {
            try
            {
                var notification = new NotificationMessageDto
                {
                    Id = Guid.NewGuid(),
                    Type = NotificationTypes.ItemSold,
                    Title = "Przedmiot sprzedany!",
                    Message = $"💰 Twój przedmiot '{eventData.ItemName}' został sprzedany za {eventData.Price:F2} {eventData.Currency}",
                    Severity = "success",
                    ActionUrl = eventData.RentalId.HasValue ? $"/rentals/{eventData.RentalId}" : "/dashboard",
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationAppService.SendToUserAsync(eventData.UserId, notification);
                _logger.LogInformation("Created item sold notification for user {UserId}, item {ItemId}",
                    eventData.UserId, eventData.ItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create item sold notification for item {ItemId}",
                    eventData.ItemId);
            }
        }
    }
}