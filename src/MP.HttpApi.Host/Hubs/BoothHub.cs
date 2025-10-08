using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Volo.Abp.Users;

namespace MP.Hubs
{
    /// <summary>
    /// SignalR hub for live booth status updates on floor plan
    /// Updates: Booth rental status, availability, conflicts prevention
    /// </summary>
    [Authorize]
    public class BoothHub : Hub
    {
        private readonly ICurrentUser _currentUser;

        public BoothHub(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        public override async Task OnConnectedAsync()
        {
            var tenantId = _currentUser.TenantId;

            if (tenantId.HasValue)
            {
                // Add to tenant booth updates group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"booths:tenant:{tenantId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var tenantId = _currentUser.TenantId;

            if (tenantId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"booths:tenant:{tenantId}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Subscribe to specific floor plan updates
        /// </summary>
        public async Task SubscribeToFloorPlan(Guid floorPlanId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"floorplan:{floorPlanId}");
        }

        /// <summary>
        /// Unsubscribe from floor plan updates
        /// </summary>
        public async Task UnsubscribeFromFloorPlan(Guid floorPlanId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"floorplan:{floorPlanId}");
        }

        /// <summary>
        /// Subscribe to specific booth updates
        /// </summary>
        public async Task SubscribeToBooth(Guid boothId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"booth:{boothId}");
        }

        /// <summary>
        /// Unsubscribe from booth updates
        /// </summary>
        public async Task UnsubscribeFromBooth(Guid boothId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"booth:{boothId}");
        }
    }
}
