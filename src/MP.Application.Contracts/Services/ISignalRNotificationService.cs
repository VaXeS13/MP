using System;
using System.Threading.Tasks;
using MP.Application.Contracts.Notifications;

namespace MP.Application.Contracts.Services
{
    /// <summary>
    /// Service for sending SignalR notifications
    /// Interface is in Application.Contracts to allow usage from Application layer
    /// Implementation is in HttpApi.Host layer to have access to Hub types
    /// </summary>
    public interface ISignalRNotificationService
    {
        Task SendItemSoldNotificationAsync(Guid userId, Guid itemId, string itemName, decimal salePrice);
        Task SendBoothStatusUpdateAsync(Guid? tenantId, Guid boothId, string status, bool isOccupied, Guid? rentalId = null, DateTime? occupiedUntil = null);
        Task SendDashboardRefreshAsync(Guid? tenantId);

        /// <summary>
        /// Sends a notification to a specific user via SignalR
        /// </summary>
        Task SendNotificationToUserAsync(Guid userId, NotificationDto notification);

        /// <summary>
        /// Sends unread count update to a specific user via SignalR
        /// </summary>
        Task SendUnreadCountUpdateAsync(Guid userId, int unreadCount);
    }
}
