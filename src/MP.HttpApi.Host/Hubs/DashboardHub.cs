using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Volo.Abp.Users;

namespace MP.Hubs
{
    /// <summary>
    /// SignalR hub for live dashboard updates
    /// Updates: Revenue, rentals count, booth occupancy, sales statistics
    /// </summary>
    [Authorize]
    public class DashboardHub : Hub
    {
        private readonly ICurrentUser _currentUser;

        public DashboardHub(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        public override async Task OnConnectedAsync()
        {
            var tenantId = _currentUser.TenantId;

            if (tenantId.HasValue)
            {
                // Add to tenant dashboard group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"dashboard:tenant:{tenantId}");
            }

            // Add to role-specific groups for admin/cashier dashboards
            if (_currentUser.IsInRole("admin"))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"dashboard:admin:{tenantId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var tenantId = _currentUser.TenantId;

            if (tenantId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"dashboard:tenant:{tenantId}");
            }

            if (_currentUser.IsInRole("admin"))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"dashboard:admin:{tenantId}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Request immediate dashboard refresh
        /// </summary>
        public async Task RequestDashboardUpdate()
        {
            // This can be called by client to request fresh data
            await Clients.Caller.SendAsync("DashboardUpdateRequested");
        }
    }
}
