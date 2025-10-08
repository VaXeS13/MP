using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using MP.Application.Contracts.SignalR;

namespace MP.Application.Contracts.Notifications
{
    /// <summary>
    /// Application service for managing and sending real-time notifications
    /// </summary>
    public interface INotificationAppService : IApplicationService
    {
        /// <summary>
        /// Send notification to specific user
        /// </summary>
        Task SendToUserAsync(Guid userId, NotificationMessageDto notification);

        /// <summary>
        /// Send notification to all users in tenant
        /// </summary>
        Task SendToTenantAsync(NotificationMessageDto notification);

        /// <summary>
        /// Get user's unread notifications
        /// </summary>
        Task<List<NotificationMessageDto>> GetUnreadNotificationsAsync();

        /// <summary>
        /// Mark notification as read
        /// </summary>
        Task MarkAsReadAsync(Guid notificationId);

        /// <summary>
        /// Get user's notification count
        /// </summary>
        Task<int> GetUnreadCountAsync();
    }
}
