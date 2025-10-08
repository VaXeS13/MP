using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.MultiTenancy;
using MP.Permissions;

namespace MP.Identity
{
    public class UserCreatedEventHandler :
        IDistributedEventHandler<EntityCreatedEto<IdentityUser>>,
        ITransientDependency
    {
        private readonly IPermissionManager _permissionManager;
        private readonly ILogger<UserCreatedEventHandler> _logger;

        public UserCreatedEventHandler(
            IPermissionManager permissionManager,
            ILogger<UserCreatedEventHandler> logger)
        {
            _permissionManager = permissionManager;
            _logger = logger;
        }

        public async Task HandleEventAsync(EntityCreatedEto<IdentityUser> eventData)
        {
            try
            {
                var user = eventData.Entity;
                var tenantId = user.TenantId;

                _logger.LogInformation("Granting default permissions for new user: {UserName} ({UserId}) in tenant: {TenantId}",
                    user.UserName, user.Id, tenantId);

                // Grant basic Booths permission to new user in their tenant context
                // The permission will be scoped to the user's tenant automatically by IPermissionManager
                await _permissionManager.SetAsync(
                    MPPermissions.Booths.Default,
                    "U", // User provider
                    user.Id.ToString(),
                    true);

                _logger.LogInformation("Successfully granted MP.Booths permission to user: {UserName} in tenant: {TenantId}",
                    user.UserName, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to grant default permissions for user: {UserId} in tenant: {TenantId}",
                    eventData.Entity.Id, eventData.Entity.TenantId);
                // Don't throw - we don't want to break user registration if permission grant fails
            }
        }
    }
}