using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Users;
using Volo.Abp.Uow;
using Volo.Abp.MultiTenancy;
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
using MP.Localization;

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
        private readonly ICurrentTenant _currentTenant;
        private readonly IRentalRepository _rentalRepository;
        private readonly IStringLocalizer<MPResource> _stringLocalizer;

        public NotificationEventHandler(
            INotificationAppService notificationAppService,
            ILogger<NotificationEventHandler> logger,
            ICurrentUser currentUser,
            ICurrentTenant currentTenant,
            IRentalRepository rentalRepository,
            IStringLocalizer<MPResource> stringLocalizer)
        {
            _notificationAppService = notificationAppService;
            _logger = logger;
            _currentUser = currentUser;
            _currentTenant = currentTenant;
            _rentalRepository = rentalRepository;
            _stringLocalizer = stringLocalizer;
        }

        public async Task HandleEventAsync(RentalConfirmedEvent eventData)
        {
            var rental = eventData.Entity;

            try
            {
                var notification = new NotificationMessageDto
                {
                    Id = Guid.NewGuid(),
                    Type = MP.Domain.Notifications.NotificationTypes.RentalStarted,
                    Title = _stringLocalizer["Notification:RentalStarted:Title"],
                    Message = _stringLocalizer["Notification:RentalStarted:Message", rental.Booth.Number, rental.Period.StartDate.ToString("dd.MM.yyyy"), rental.Period.EndDate.ToString("dd.MM.yyyy")],
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
                    Type = MP.Domain.Notifications.NotificationTypes.RentalCompleted,
                    Title = _stringLocalizer["Notification:RentalCompleted:Title"],
                    Message = _stringLocalizer["Notification:RentalCompleted:Message", rental.Booth.Number],
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
                    Type = MP.Domain.Notifications.NotificationTypes.RentalCompleted,
                    Title = _stringLocalizer["Notification:RentalCancelled:Title"],
                    Message = _stringLocalizer["Notification:RentalCancelled:Message", rental.Booth.Number],
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
                    Type = MP.Domain.Notifications.NotificationTypes.RentalExtended,
                    Title = _stringLocalizer["Notification:RentalExtended:Title"],
                    Message = _stringLocalizer["Notification:RentalExtended:Message", rental.Booth.Number, rental.Period.EndDate.ToString("dd.MM.yyyy")],
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

        [UnitOfWork]
        public async Task HandleEventAsync(PaymentInitiatedEvent eventData)
        {
            // Set tenant context from event
            using (_currentTenant.Change(eventData.TenantId))
            {
                try
                {
                    // Build message with booth names/numbers
                    string boothInfo = "unknown booths";

                if (eventData.RentalIds.Any())
                {
                    try
                    {
                        var rentals = await _rentalRepository.GetListAsync(
                            r => eventData.RentalIds.Contains(r.Id),
                            includeDetails: true
                        );

                        if (rentals.Any())
                        {
                            var boothNumbers = rentals
                                .Select(r => r.Booth.Number)
                                .Distinct()
                                .ToList();

                            if (boothNumbers.Count == 1)
                                boothInfo = $"booth {boothNumbers[0]}";
                            else
                                boothInfo = $"booths {string.Join(", ", boothNumbers)}";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to load rental details for booth info in payment notification");
                    }
                }

                var notification = new NotificationMessageDto
                {
                    Id = Guid.NewGuid(),
                    Type = MP.Domain.Notifications.NotificationTypes.PaymentReceived,
                    Title = _stringLocalizer["Notification:PaymentInitiated:Title"],
                    Message = _stringLocalizer["Notification:PaymentInitiated:Message", boothInfo, eventData.Amount.ToString("F2"), eventData.Currency, eventData.SessionId],
                    Severity = "info",
                    ActionUrl = $"/rentals/my-rentals",
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationAppService.SendToUserAsync(eventData.UserId, notification);
                _logger.LogInformation("Created payment initiated notification for user {UserId}, session {SessionId}",
                    eventData.UserId, eventData.SessionId);
            }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create payment initiated notification for session {SessionId}",
                        eventData.SessionId);
                }
            }
        }

        [UnitOfWork]
        public async Task HandleEventAsync(PaymentCompletedEvent eventData)
        {
            _logger.LogInformation("[EVENT HANDLER] Received PaymentCompletedEvent for user {UserId}, transaction {TransactionId}",
                eventData.UserId, eventData.TransactionId);

            // Set tenant context from event
            using (_currentTenant.Change(eventData.TenantId))
            {
                try
                {
                    var notification = new NotificationMessageDto
                    {
                        Id = Guid.NewGuid(),
                        Type = MP.Domain.Notifications.NotificationTypes.PaymentReceived,
                        Title = _stringLocalizer["Notification:PaymentCompleted:Title"],
                        Message = _stringLocalizer["Notification:PaymentCompleted:Message", eventData.RentalIds.Count, eventData.Amount.ToString("F2"), eventData.Currency, eventData.TransactionId],
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
        }

        [UnitOfWork]
        public async Task HandleEventAsync(PaymentFailedEvent eventData)
        {
            // Set tenant context from event
            using (_currentTenant.Change(eventData.TenantId))
            {
                try
                {
                    var notification = new NotificationMessageDto
                    {
                        Id = Guid.NewGuid(),
                        Type = MP.Domain.Notifications.NotificationTypes.PaymentFailed,
                        Title = _stringLocalizer["Notification:PaymentFailed:Title"],
                        Message = _stringLocalizer["Notification:PaymentFailed:Message", eventData.Reason],
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
        }

        public async Task HandleEventAsync(ItemSoldEvent eventData)
        {
            try
            {
                var notification = new NotificationMessageDto
                {
                    Id = Guid.NewGuid(),
                    Type = MP.Domain.Notifications.NotificationTypes.ItemSold,
                    Title = _stringLocalizer["Notification:ItemSold:Title"],
                    Message = _stringLocalizer["Notification:ItemSold:Message", eventData.ItemName, eventData.Price.ToString("F2"), eventData.Currency],
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