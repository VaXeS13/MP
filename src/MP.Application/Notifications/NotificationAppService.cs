using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Volo.Abp.Users;
using Volo.Abp.MultiTenancy;
using MP.Application.Contracts.Notifications;
using MP.Application.Contracts.Services;
using MP.Application.Contracts.SignalR;
using MP.Domain.Notifications;
using MP.Domain.OrganizationalUnits;

namespace MP.Application.Notifications
{
    [Authorize]
    [Route("api/app/notification")]
    public class NotificationAppService : ApplicationService, INotificationAppService
    {
        private readonly IUserNotificationRepository _userNotificationRepository;
        private readonly IRepository<UserNotification, Guid> _notificationRepository;
        private readonly ICurrentUser _currentUser;
        private readonly ICurrentTenant _currentTenant;
        private readonly ICurrentOrganizationalUnit _currentOrganizationalUnit;
        private readonly ISignalRNotificationService _signalRNotificationService;

        public NotificationAppService(
            IUserNotificationRepository userNotificationRepository,
            IRepository<UserNotification, Guid> notificationRepository,
            ICurrentUser currentUser,
            ICurrentTenant currentTenant,
            ICurrentOrganizationalUnit currentOrganizationalUnit,
            ISignalRNotificationService signalRNotificationService)
        {
            _userNotificationRepository = userNotificationRepository;
            _notificationRepository = notificationRepository;
            _currentUser = currentUser;
            _currentTenant = currentTenant;
            _currentOrganizationalUnit = currentOrganizationalUnit;
            _signalRNotificationService = signalRNotificationService;
        }

        [HttpPost("send-to-user")]
        [AllowAnonymous] // Required for Hangfire background jobs (no authenticated user in background context)
        [UnitOfWork] // Ensures database changes are committed
        public async Task SendToUserAsync(Guid userId, NotificationMessageDto notification)
        {
            // Map severity from string to enum
            var severity = ParseDomainSeverity(notification.Severity);

            // Get TenantId - prefer CurrentTenant (works in Hangfire context) with fallback to CurrentUser
            var tenantId = _currentTenant.Id ?? _currentUser.TenantId;
            var organizationalUnitId = _currentOrganizationalUnit.Id;

            Logger.LogInformation("[NOTIFICATION] Sending notification to user {UserId} in tenant {TenantId}, unit {UnitId}. Type={Type}, Title={Title}",
                userId, tenantId, organizationalUnitId, notification.Type, notification.Title);

            var userNotification = new UserNotification(
                GuidGenerator.Create(),
                userId,
                notification.Type,
                notification.Title,
                notification.Message,
                severity,
                notification.ActionUrl,
                null,
                null,
                null,
                organizationalUnitId,
                tenantId
            );

            // 1. Save to database
            await _notificationRepository.InsertAsync(userNotification);

            // 2. Map to DTO for SignalR
            var notificationDto = new NotificationDto
            {
                Id = userNotification.Id,
                Type = userNotification.Type,
                Title = userNotification.Title,
                Message = userNotification.Message,
                Severity = ParseContractSeverity(userNotification.Severity),
                IsRead = userNotification.IsRead,
                ReadAt = userNotification.ReadAt,
                ActionUrl = userNotification.ActionUrl,
                RelatedEntityType = userNotification.RelatedEntityType,
                RelatedEntityId = userNotification.RelatedEntityId,
                CreationTime = userNotification.CreationTime,
                ExpiresAt = userNotification.ExpiresAt
            };

            // 3. Send via SignalR in real-time
            await _signalRNotificationService.SendNotificationToUserAsync(userId, notificationDto);

            // 4. Update unread count via SignalR
            var unreadCount = await _userNotificationRepository.GetUnreadCountAsync(userId);
            await _signalRNotificationService.SendUnreadCountUpdateAsync(userId, unreadCount);
        }

        [HttpPost("send-to-tenant")]
        public async Task SendToTenantAsync(NotificationMessageDto notification)
        {
            // This would typically be implemented to send to all users in a tenant
            // For now, we'll implement a basic version
            throw new NotImplementedException("Send to all users in tenant not yet implemented");
        }

        [HttpGet]
        public async Task<NotificationListDto> GetListAsync(GetNotificationsInput input)
        {
            var userId = _currentUser.GetId();

            var notifications = await _userNotificationRepository.GetUserNotificationsAsync(
                userId,
                input.IsRead,
                input.IncludeExpired,
                input.SkipCount,
                input.MaxResultCount > 0 ? input.MaxResultCount : 10
            );

            var notificationDtos = ObjectMapper.Map<List<UserNotification>, List<NotificationDto>>(notifications);

            var totalCount = await _userNotificationRepository.GetTotalCountAsync(userId, input.IsRead, input.IncludeExpired);
            var unreadCount = await _userNotificationRepository.GetUnreadCountAsync(userId);

            return new NotificationListDto
            {
                Items = notificationDtos,
                TotalCount = totalCount,
                UnreadCount = unreadCount
            };
        }

        [HttpGet("unread")]
        public async Task<NotificationListDto> GetUnreadAsync(GetNotificationsInput input)
        {
            var userId = _currentUser.GetId();

            var notifications = await _userNotificationRepository.GetUserNotificationsAsync(
                userId,
                false, // Only unread
                input.IncludeExpired,
                input.SkipCount,
                input.MaxResultCount > 0 ? input.MaxResultCount : 10
            );

            var notificationDtos = ObjectMapper.Map<List<UserNotification>, List<NotificationDto>>(notifications);

            var totalCount = await _userNotificationRepository.GetTotalCountAsync(userId, false, input.IncludeExpired);
            var unreadCount = await _userNotificationRepository.GetUnreadCountAsync(userId);

            return new NotificationListDto
            {
                Items = notificationDtos,
                TotalCount = totalCount,
                UnreadCount = unreadCount
            };
        }

        [HttpGet("all")]
        public async Task<NotificationListDto> GetAllAsync(GetNotificationsInput input)
        {
            var userId = _currentUser.GetId();

            var notifications = await _userNotificationRepository.GetUserNotificationsAsync(
                userId,
                null, // Both read and unread
                input.IncludeExpired,
                input.SkipCount,
                input.MaxResultCount > 0 ? input.MaxResultCount : 10
            );

            var notificationDtos = ObjectMapper.Map<List<UserNotification>, List<NotificationDto>>(notifications);

            var totalCount = await _userNotificationRepository.GetTotalCountAsync(userId, null, input.IncludeExpired);
            var unreadCount = await _userNotificationRepository.GetUnreadCountAsync(userId);

            return new NotificationListDto
            {
                Items = notificationDtos,
                TotalCount = totalCount,
                UnreadCount = unreadCount
            };
        }

        [HttpPost("{notificationId}/mark-as-read")]
        public async Task MarkAsReadAsync(Guid notificationId)
        {
            var userId = _currentUser.GetId();

            var notification = await _notificationRepository.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notification != null && !notification.IsRead)
            {
                notification.MarkAsRead();
                await _notificationRepository.UpdateAsync(notification);

                // Update unread count via SignalR
                var unreadCount = await _userNotificationRepository.GetUnreadCountAsync(userId);
                await _signalRNotificationService.SendUnreadCountUpdateAsync(userId, unreadCount);
            }
        }

        [HttpPost("mark-multiple-as-read")]
        public async Task MarkMultipleAsReadAsync(List<Guid> notificationIds)
        {
            var userId = _currentUser.GetId();
            var updated = false;

            foreach (var notificationId in notificationIds)
            {
                var notification = await _notificationRepository.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
                if (notification != null && !notification.IsRead)
                {
                    notification.MarkAsRead();
                    await _notificationRepository.UpdateAsync(notification);
                    updated = true;
                }
            }

            // Update unread count via SignalR if any notifications were marked as read
            if (updated)
            {
                var unreadCount = await _userNotificationRepository.GetUnreadCountAsync(userId);
                await _signalRNotificationService.SendUnreadCountUpdateAsync(userId, unreadCount);
            }
        }

        [HttpPost("mark-all-as-read")]
        public async Task MarkAllAsReadAsync()
        {
            var userId = _currentUser.GetId();

            await _userNotificationRepository.MarkAllAsReadAsync(userId);

            // Update unread count to 0 via SignalR
            await _signalRNotificationService.SendUnreadCountUpdateAsync(userId, 0);
        }

        [HttpGet("stats")]
        public async Task<NotificationStatsDto> GetStatsAsync()
        {
            var userId = _currentUser.GetId();

            // Get counts directly from repository instead of fetching all records
            var totalCount = await _userNotificationRepository.GetTotalCountAsync(userId, null, true);
            var unreadCount = await _userNotificationRepository.GetUnreadCountAsync(userId);
            var readCount = totalCount - unreadCount;

            var expiredNotifications = await _userNotificationRepository.GetExpiredNotificationsAsync();
            var expiredCount = expiredNotifications.Count(n => n.UserId == userId);

            return new NotificationStatsDto
            {
                TotalCount = totalCount,
                UnreadCount = unreadCount,
                ReadCount = readCount,
                ExpiredCount = expiredCount
            };
        }

        [HttpDelete("{notificationId}")]
        public async Task DeleteAsync(Guid notificationId)
        {
            var userId = _currentUser.GetId();

            var notification = await _notificationRepository.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notification != null)
            {
                await _notificationRepository.DeleteAsync(notification);
            }
        }

        [HttpDelete("expired")]
        public async Task<int> DeleteExpiredNotificationsAsync()
        {
            // This method is not available in the simplified repository
            // For now, return 0 - this can be implemented later if needed
            return 0;
        }

        [HttpGet("unread-count")]
        public async Task<int> GetUnreadCountAsync()
        {
            var userId = _currentUser.GetId();
            return await _userNotificationRepository.GetUnreadCountAsync(userId);
        }

        private Application.Contracts.Notifications.NotificationSeverity ParseContractSeverity(MP.Domain.Notifications.NotificationSeverity severity)
        {
            return severity switch
            {
                MP.Domain.Notifications.NotificationSeverity.Success => Application.Contracts.Notifications.NotificationSeverity.Success,
                MP.Domain.Notifications.NotificationSeverity.Warning => Application.Contracts.Notifications.NotificationSeverity.Warning,
                MP.Domain.Notifications.NotificationSeverity.Error => Application.Contracts.Notifications.NotificationSeverity.Error,
                _ => Application.Contracts.Notifications.NotificationSeverity.Info
            };
        }

        private MP.Domain.Notifications.NotificationSeverity ParseDomainSeverity(string severity)
        {
            return severity?.ToLower() switch
            {
                "success" => MP.Domain.Notifications.NotificationSeverity.Success,
                "warning" => MP.Domain.Notifications.NotificationSeverity.Warning,
                "error" => MP.Domain.Notifications.NotificationSeverity.Error,
                _ => MP.Domain.Notifications.NotificationSeverity.Info
            };
        }
    }
}