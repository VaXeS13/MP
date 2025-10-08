using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Volo.Abp.Users;

namespace MP.Hubs
{
    /// <summary>
    /// SignalR hub for live sales updates
    /// Clients see their items sold immediately in dashboard
    /// </summary>
    [Authorize]
    public class SalesHub : Hub
    {
        private readonly ICurrentUser _currentUser;

        public SalesHub(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = _currentUser.Id;
            var tenantId = _currentUser.TenantId;

            if (userId.HasValue)
            {
                // Add to user's sales group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"sales:user:{userId}");
            }

            if (tenantId.HasValue)
            {
                // Add to tenant sales group for admin/cashier
                await Groups.AddToGroupAsync(Context.ConnectionId, $"sales:tenant:{tenantId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = _currentUser.Id;
            var tenantId = _currentUser.TenantId;

            if (userId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sales:user:{userId}");
            }

            if (tenantId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sales:tenant:{tenantId}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Subscribe to specific rental sales updates
        /// </summary>
        public async Task SubscribeToRentalSales(Guid rentalId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"sales:rental:{rentalId}");
        }

        /// <summary>
        /// Unsubscribe from rental sales updates
        /// </summary>
        public async Task UnsubscribeFromRentalSales(Guid rentalId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sales:rental:{rentalId}");
        }
    }
}
