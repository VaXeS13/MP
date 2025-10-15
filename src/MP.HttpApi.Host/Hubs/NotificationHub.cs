using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Volo.Abp.Users;
using MP.Application.Contracts.Notifications;
using MP.Application.Contracts.SignalR;

namespace MP.Hubs
{
    /// <summary>
    /// SignalR hub for real-time notifications
    /// Supports: Item sold, rental updates, payment confirmations, settlements
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ICurrentUser _currentUser;

        public NotificationHub(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = _currentUser.Id;
            var tenantId = _currentUser.TenantId;

            if (userId.HasValue)
            {
                // Add user to their personal group for targeted notifications
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
            }

            if (tenantId.HasValue)
            {
                // Add to tenant group for tenant-wide broadcasts
                await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = _currentUser.Id;
            var tenantId = _currentUser.TenantId;

            if (userId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");
            }

            if (tenantId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Mark notification as read
        /// </summary>
        public async Task MarkAsRead(Guid notificationId)
        {
            // Client can call this to acknowledge notification receipt
            await Clients.Caller.SendAsync("NotificationMarkedAsRead", notificationId);
        }

        /// <summary>
        /// Mark multiple notifications as read
        /// </summary>
        public async Task MarkMultipleAsRead(List<Guid> notificationIds)
        {
            // Client can call this to acknowledge multiple notifications
            await Clients.Caller.SendAsync("MultipleNotificationsMarkedAsRead", notificationIds);
        }

        /// <summary>
        /// Mark all notifications as read for current user
        /// </summary>
        public async Task MarkAllAsRead()
        {
            // Client can call this to clear all notifications
            await Clients.Caller.SendAsync("AllNotificationsMarkedAsRead");
        }

        /// <summary>
        /// Get unread notification count
        /// </summary>
        public async Task GetUnreadCount()
        {
            // Server will send the unread count to the client
            await Clients.Caller.SendAsync("UnreadCountUpdated", 0); // Will be populated by service
        }

        /// <summary>
        /// Send notification to specific user (called by server-side services)
        /// </summary>
        public async Task SendToUserAsync(Guid userId, NotificationMessageDto notification)
        {
            await Clients.Group($"user:{userId}").SendAsync("ReceiveNotification", notification);
        }

        /// <summary>
        /// Send notification to all users in tenant (called by server-side services)
        /// </summary>
        public async Task SendToTenantAsync(Guid tenantId, NotificationMessageDto notification)
        {
            await Clients.Group($"tenant:{tenantId}").SendAsync("ReceiveNotification", notification);
        }

        /// <summary>
        /// Update unread count for specific user
        /// </summary>
        public async Task UpdateUnreadCountAsync(Guid userId, int unreadCount)
        {
            await Clients.Group($"user:{userId}").SendAsync("UnreadCountUpdated", unreadCount);
        }

        /// <summary>
        /// Send notification list updates to user
        /// </summary>
        public async Task SendNotificationListUpdate(Guid userId, NotificationListDto notificationList)
        {
            await Clients.Group($"user:{userId}").SendAsync("NotificationListUpdated", notificationList);
        }

        /// <summary>
        /// Send notification statistics to user
        /// </summary>
        public async Task SendNotificationStats(Guid userId, NotificationStatsDto stats)
        {
            await Clients.Group($"user:{userId}").SendAsync("NotificationStatsUpdated", stats);
        }
    }
}
