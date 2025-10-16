using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
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

        public SignalRNotificationService(
            IHubContext<NotificationHub> notificationHub,
            IHubContext<BoothHub> boothHub,
            IHubContext<DashboardHub> dashboardHub,
            IHubContext<SalesHub> salesHub)
        {
            _notificationHub = notificationHub;
            _boothHub = boothHub;
            _dashboardHub = dashboardHub;
            _salesHub = salesHub;
        }

        public async Task SendItemSoldNotificationAsync(Guid userId, Guid itemId, string itemName, decimal salePrice)
        {
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
        }

        public async Task SendBoothStatusUpdateAsync(Guid? tenantId, Guid boothId, string status, bool isOccupied, Guid? rentalId = null, DateTime? occupiedUntil = null)
        {
            if (!tenantId.HasValue)
                return;

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
        }

        public async Task SendDashboardRefreshAsync(Guid? tenantId)
        {
            if (!tenantId.HasValue)
                return;

            await _dashboardHub.Clients
                .Group($"dashboard:tenant:{tenantId}")
                .SendAsync("DashboardRefreshNeeded");
        }

        public async Task SendNotificationToUserAsync(Guid userId, NotificationDto notification)
        {
            await _notificationHub.Clients
                .Group($"user:{userId}")
                .SendAsync("ReceiveNotification", notification);
        }

        public async Task SendUnreadCountUpdateAsync(Guid userId, int unreadCount)
        {
            await _notificationHub.Clients
                .Group($"user:{userId}")
                .SendAsync("UnreadCountUpdated", unreadCount);
        }
    }
}
