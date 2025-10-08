using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Volo.Abp.Users;

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
    }
}
