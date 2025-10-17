using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MP.Hubs;
using MP.Application.Contracts.SignalR;
using MP.Application.Contracts.Services;
using MP.Application.Contracts.Notifications;
using Volo.Abp.DependencyInjection;

namespace MP.Services
{
    /// <summary>
    /// Service for sending SignalR notifications
    /// This service is in HttpApi.Host layer to have access to Hub types
    /// </summary>
    public class SignalRNotificationService : ISignalRNotificationService, ITransientDependency
    {
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly IHubContext<BoothHub> _boothHub;
        private readonly IHubContext<DashboardHub> _dashboardHub;
        private readonly IHubContext<SalesHub> _salesHub;
        private readonly ILogger<SignalRNotificationService> _logger;

        public SignalRNotificationService(
            IHubContext<NotificationHub> notificationHub,
            IHubContext<BoothHub> boothHub,
            IHubContext<DashboardHub> dashboardHub,
            IHubContext<SalesHub> salesHub,
            ILogger<SignalRNotificationService> logger)
        {
            _notificationHub = notificationHub;
            _boothHub = boothHub;
            _dashboardHub = dashboardHub;
            _salesHub = salesHub;
            _logger = logger;
        }

        public async Task SendItemSoldNotificationAsync(Guid userId, Guid itemId, string itemName, decimal salePrice)
        {
            try
            {
                _logger.LogInformation("[SignalR] Sending item sold notification to user {UserId}, item {ItemName}",
                    userId, itemName);

                var notification = new NotificationMessageDto
                {
                    Id = Guid.NewGuid(),
                    Type = "ItemSold",
                    Title = "Item Sold!",
                    Message = $"Your item '{itemName}' has been sold for {salePrice:C} PLN",
                    Severity = "success",
                    CreatedAt = DateTime.UtcNow,
                    ActionUrl = null
                };

                // Send to user's notification hub
                await _notificationHub.Clients
                    .Group($"user:{userId}")
                    .SendAsync("ReceiveNotification", notification);

                _logger.LogInformation("[SignalR] Item sold notification sent to user {UserId}", userId);

                // Send to user's sales hub
                var itemSoldDto = new ItemSoldDto
                {
                    ItemId = itemId,
                    ItemName = itemName,
                    SalePrice = salePrice,
                    SoldAt = DateTime.UtcNow,
                    RentalId = Guid.Empty // Will be set by caller if available
                };

                await _salesHub.Clients
                    .Group($"sales:user:{userId}")
                    .SendAsync("ItemSold", itemSoldDto);

                _logger.LogInformation("[SignalR] Item sold event sent to sales hub for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalR] Error sending item sold notification to user {UserId}, item {ItemId}",
                    userId, itemId);
                throw;
            }
        }

        public async Task SendBoothStatusUpdateAsync(Guid? tenantId, Guid boothId, string status, bool isOccupied, Guid? rentalId = null, DateTime? occupiedUntil = null)
        {
            try
            {
                if (!tenantId.HasValue)
                {
                    _logger.LogWarning("[SignalR] SendBoothStatusUpdateAsync called with null tenantId");
                    return;
                }

                _logger.LogInformation("[SignalR] Sending booth status update to tenant {TenantId}, booth {BoothId}, status {Status}",
                    tenantId, boothId, status);

                var update = new BoothStatusUpdateDto
                {
                    BoothId = boothId,
                    Status = status,
                    IsOccupied = isOccupied,
                    CurrentRentalId = rentalId,
                    OccupiedUntil = occupiedUntil
                };

                await _boothHub.Clients
                    .Group($"booths:tenant:{tenantId}")
                    .SendAsync("BoothStatusUpdated", update);

                _logger.LogInformation("[SignalR] Booth status update sent to booth hub for tenant {TenantId}", tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalR] Error sending booth status update for tenant {TenantId}, booth {BoothId}",
                    tenantId, boothId);
                throw;
            }
        }

        public async Task SendDashboardRefreshAsync(Guid? tenantId)
        {
            try
            {
                if (!tenantId.HasValue)
                {
                    _logger.LogWarning("[SignalR] SendDashboardRefreshAsync called with null tenantId");
                    return;
                }

                _logger.LogInformation("[SignalR] Sending dashboard refresh to tenant {TenantId}", tenantId);

                await _dashboardHub.Clients
                    .Group($"dashboard:tenant:{tenantId}")
                    .SendAsync("DashboardRefreshNeeded");

                _logger.LogInformation("[SignalR] Dashboard refresh sent to tenant {TenantId}", tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalR] Error sending dashboard refresh to tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task SendNotificationToUserAsync(Guid userId, NotificationDto notification)
        {
            try
            {
                _logger.LogInformation("[SignalR] Sending notification to user {UserId}, type {Type}, title {Title}",
                    userId, notification.Type, notification.Title);

                var groupName = $"user:{userId}";
                _logger.LogDebug("[SignalR] Target group: {GroupName}", groupName);

                await _notificationHub.Clients
                    .Group(groupName)
                    .SendAsync("ReceiveNotification", notification);

                _logger.LogInformation("[SignalR] Notification sent to user {UserId} successfully", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalR] Error sending notification to user {UserId}, type {Type}",
                    userId, notification?.Type);
                throw;
            }
        }

        public async Task SendUnreadCountUpdateAsync(Guid userId, int unreadCount)
        {
            try
            {
                _logger.LogInformation("[SignalR] Sending unread count update to user {UserId}, count {UnreadCount}",
                    userId, unreadCount);

                var groupName = $"user:{userId}";
                _logger.LogDebug("[SignalR] Target group: {GroupName}, unread count: {UnreadCount}", groupName, unreadCount);

                await _notificationHub.Clients
                    .Group(groupName)
                    .SendAsync("UnreadCountUpdated", unreadCount);

                _logger.LogInformation("[SignalR] Unread count update sent to user {UserId} successfully", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalR] Error sending unread count update to user {UserId}",
                    userId);
                throw;
            }
        }
    }
}
