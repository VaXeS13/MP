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
        /// Get paginated notifications for current user
        /// </summary>
        Task<NotificationListDto> GetListAsync(GetNotificationsInput input);

        /// <summary>
        /// Get unread notifications for current user
        /// </summary>
        Task<NotificationListDto> GetUnreadAsync(GetNotificationsInput input);

        /// <summary>
        /// Get all notifications for current user
        /// </summary>
        Task<NotificationListDto> GetAllAsync(GetNotificationsInput input);

        /// <summary>
        /// Mark notification as read
        /// </summary>
        Task MarkAsReadAsync(Guid notificationId);

        /// <summary>
        /// Mark multiple notifications as read
        /// </summary>
        Task MarkMultipleAsReadAsync(List<Guid> notificationIds);

        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        Task MarkAllAsReadAsync();

        /// <summary>
        /// Get notification statistics
        /// </summary>
        Task<NotificationStatsDto> GetStatsAsync();

        /// <summary>
        /// Delete notification
        /// </summary>
        Task DeleteAsync(Guid notificationId);

        /// <summary>
        /// Delete expired notifications
        /// </summary>
        Task<int> DeleteExpiredNotificationsAsync();

        /// <summary>
        /// Get user's notification count
        /// </summary>
        Task<int> GetUnreadCountAsync();
    }
}
