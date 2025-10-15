using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;
using MP.Application.Contracts.Notifications;
using MP.Application.Contracts.SignalR;
using MP.Domain.Notifications;

namespace MP.Application.Notifications
{
    [Authorize]
    public class NotificationAppService : ApplicationService, INotificationAppService
    {
        private readonly IUserNotificationRepository _userNotificationRepository;
        private readonly IRepository<UserNotification, Guid> _notificationRepository;
        private readonly ICurrentUser _currentUser;

        public NotificationAppService(
            IUserNotificationRepository userNotificationRepository,
            IRepository<UserNotification, Guid> notificationRepository,
            ICurrentUser currentUser)
        {
            _userNotificationRepository = userNotificationRepository;
            _notificationRepository = notificationRepository;
            _currentUser = currentUser;
        }

        public async Task SendToUserAsync(Guid userId, NotificationMessageDto notification)
        {
            var userNotification = new UserNotification(
                GuidGenerator.Create(),
                userId,
                notification.Type,
                notification.Title,
                notification.Message,
                MP.Domain.Notifications.NotificationSeverity.Info, // Temporary fix - use default severity
                notification.ActionUrl,
                null,
                null,
                null,
                _currentUser.TenantId
            );

            await _notificationRepository.InsertAsync(userNotification);
        }

        public async Task SendToTenantAsync(NotificationMessageDto notification)
        {
            // This would typically be implemented to send to all users in a tenant
            // For now, we'll implement a basic version
            throw new NotImplementedException("Send to all users in tenant not yet implemented");
        }

        public async Task<NotificationListDto> GetListAsync(GetNotificationsInput input)
        {
            var userId = _currentUser.GetId();

            var notifications = await _userNotificationRepository.GetUserNotificationsAsync(
                userId,
                input.IsRead,
                input.IncludeExpired,
                input.MaxResultCount > 0 ? input.MaxResultCount : 10
            );

            var notificationDtos = ObjectMapper.Map<List<UserNotification>, List<NotificationDto>>(notifications);

            var unreadCount = await _userNotificationRepository.GetUnreadCountAsync(userId);

            return new NotificationListDto
            {
                Items = notificationDtos,
                TotalCount = notifications.Count,
                UnreadCount = unreadCount
            };
        }

        public async Task<NotificationListDto> GetUnreadAsync(GetNotificationsInput input)
        {
            var userId = _currentUser.GetId();

            var notifications = await _userNotificationRepository.GetUserNotificationsAsync(
                userId,
                false, // Only unread
                input.IncludeExpired,
                input.MaxResultCount > 0 ? input.MaxResultCount : 10
            );

            var notificationDtos = ObjectMapper.Map<List<UserNotification>, List<NotificationDto>>(notifications);

            var unreadCount = await _userNotificationRepository.GetUnreadCountAsync(userId);

            return new NotificationListDto
            {
                Items = notificationDtos,
                TotalCount = notifications.Count,
                UnreadCount = unreadCount
            };
        }

        public async Task<NotificationListDto> GetAllAsync(GetNotificationsInput input)
        {
            var userId = _currentUser.GetId();

            var notifications = await _userNotificationRepository.GetUserNotificationsAsync(
                userId,
                null, // Both read and unread
                input.IncludeExpired,
                input.MaxResultCount > 0 ? input.MaxResultCount : 10
            );

            var notificationDtos = ObjectMapper.Map<List<UserNotification>, List<NotificationDto>>(notifications);

            var unreadCount = await _userNotificationRepository.GetUnreadCountAsync(userId);

            return new NotificationListDto
            {
                Items = notificationDtos,
                TotalCount = notifications.Count,
                UnreadCount = unreadCount
            };
        }

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            var userId = _currentUser.GetId();

            var notification = await _notificationRepository.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notification != null)
            {
                notification.MarkAsRead();
                await _notificationRepository.UpdateAsync(notification);
            }
        }

        public async Task MarkMultipleAsReadAsync(List<Guid> notificationIds)
        {
            var userId = _currentUser.GetId();

            foreach (var notificationId in notificationIds)
            {
                var notification = await _notificationRepository.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
                if (notification != null)
                {
                    notification.MarkAsRead();
                    await _notificationRepository.UpdateAsync(notification);
                }
            }
        }

        public async Task MarkAllAsReadAsync()
        {
            var userId = _currentUser.GetId();

            await _userNotificationRepository.MarkAllAsReadAsync(userId);
        }

        public async Task<NotificationStatsDto> GetStatsAsync()
        {
            var userId = _currentUser.GetId();

            var allNotifications = await _userNotificationRepository.GetUserNotificationsAsync(
                userId,
                null,
                true, // Include expired
                int.MaxValue // Get all
            );

            var unreadCount = await _userNotificationRepository.GetUnreadCountAsync(userId);
            var expiredNotifications = await _userNotificationRepository.GetExpiredNotificationsAsync();
            var expiredCount = expiredNotifications.Count(n => n.UserId == userId);

            return new NotificationStatsDto
            {
                TotalCount = allNotifications.Count,
                UnreadCount = unreadCount,
                ReadCount = allNotifications.Count - unreadCount,
                ExpiredCount = expiredCount
            };
        }

        public async Task DeleteAsync(Guid notificationId)
        {
            var userId = _currentUser.GetId();

            var notification = await _notificationRepository.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notification != null)
            {
                await _notificationRepository.DeleteAsync(notification);
            }
        }

        public async Task<int> DeleteExpiredNotificationsAsync()
        {
            // This method is not available in the simplified repository
            // For now, return 0 - this can be implemented later if needed
            return 0;
        }

        public async Task<int> GetUnreadCountAsync()
        {
            var userId = _currentUser.GetId();
            return await _userNotificationRepository.GetUnreadCountAsync(userId);
        }

        private Application.Contracts.Notifications.NotificationSeverity ParseSeverity(string severity)
        {
            return severity?.ToLower() switch
            {
                "success" => Application.Contracts.Notifications.NotificationSeverity.Success,
                "warning" => Application.Contracts.Notifications.NotificationSeverity.Warning,
                "error" => Application.Contracts.Notifications.NotificationSeverity.Error,
                _ => Application.Contracts.Notifications.NotificationSeverity.Info
            };
        }
    }
}