using Microsoft.EntityFrameworkCore;
using MP.Domain.Notifications;
using MP.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace MP.EntityFrameworkCore.Notifications
{
    public class EfCoreUserNotificationRepository : EfCoreRepository<MPDbContext, UserNotification, Guid>, IUserNotificationRepository
    {
        public EfCoreUserNotificationRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<List<UserNotification>> GetUserNotificationsAsync(
            Guid userId,
            bool? isRead = null,
            bool includeExpired = false,
            int maxResultCount = 50,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var query = dbContext.UserNotifications
                .Where(n => n.UserId == userId);

            if (isRead.HasValue)
            {
                query = query.Where(n => n.IsRead == isRead.Value);
            }

            if (!includeExpired)
            {
                var now = DateTime.Now;
                query = query.Where(n => n.ExpiresAt == null || n.ExpiresAt > now);
            }

            return await query
                .OrderByDescending(n => n.CreationTime)
                .Take(maxResultCount)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetUnreadCountAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var now = DateTime.Now;

            return await dbContext.UserNotifications
                .Where(n => n.UserId == userId &&
                           !n.IsRead &&
                           (n.ExpiresAt == null || n.ExpiresAt > now))
                .CountAsync(cancellationToken);
        }

        public async Task<int> MarkAllAsReadAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var now = DateTime.Now;

            var notifications = await dbContext.UserNotifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync(cancellationToken);

            foreach (var notification in notifications)
            {
                notification.MarkAsRead();
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return notifications.Count;
        }

        public async Task<List<UserNotification>> GetExpiredNotificationsAsync(
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var now = DateTime.Now;

            return await dbContext.UserNotifications
                .Where(n => n.ExpiresAt != null && n.ExpiresAt < now)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<UserNotification>> GetUnreadByUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await GetUserNotificationsAsync(userId, isRead: false, includeExpired: false, cancellationToken: cancellationToken);
        }

        public async Task<int> GetUnreadCountByUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await GetUnreadCountAsync(userId, cancellationToken);
        }
    }
}
