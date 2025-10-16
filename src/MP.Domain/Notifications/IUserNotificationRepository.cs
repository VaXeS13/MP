using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.Notifications
{
    /// <summary>
    /// User notification repository
    /// </summary>
    public interface IUserNotificationRepository : IRepository<UserNotification, Guid>
    {
        Task<List<UserNotification>> GetUserNotificationsAsync(
            Guid userId,
            bool? isRead = null,
            bool includeExpired = false,
            int skipCount = 0,
            int maxResultCount = 50,
            CancellationToken cancellationToken = default);

        Task<int> GetTotalCountAsync(
            Guid userId,
            bool? isRead = null,
            bool includeExpired = false,
            CancellationToken cancellationToken = default);

        Task<int> GetUnreadCountAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<int> MarkAllAsReadAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<List<UserNotification>> GetExpiredNotificationsAsync(
            CancellationToken cancellationToken = default);

        Task<List<UserNotification>> GetUnreadByUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<int> GetUnreadCountByUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}
