using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.LocalAgent
{
    /// <summary>
    /// Repository interface for AgentApiKey entities
    /// </summary>
    public interface IAgentApiKeyRepository : IRepository<AgentApiKey, Guid>
    {
        /// <summary>
        /// Find an API key by its hash
        /// Used for authentication during SignalR connection
        /// </summary>
        Task<AgentApiKey?> FindByKeyHashAsync(string keyHash, Guid tenantId);

        /// <summary>
        /// Find all active API keys for an agent
        /// </summary>
        Task<List<AgentApiKey>> GetActiveKeysForAgentAsync(string agentId, Guid tenantId);

        /// <summary>
        /// Find all API keys for a tenant
        /// </summary>
        Task<List<AgentApiKey>> GetAllKeysForTenantAsync(Guid tenantId);

        /// <summary>
        /// Find an API key by agent ID and suffix
        /// Used for key identification and rotation
        /// </summary>
        Task<AgentApiKey?> FindByAgentAndSuffixAsync(string agentId, string suffix, Guid tenantId);

        /// <summary>
        /// Get all expired API keys
        /// Used for cleanup and rotation reminders
        /// </summary>
        Task<List<AgentApiKey>> GetExpiredKeysAsync();

        /// <summary>
        /// Get all keys that are near expiration (10 days or less)
        /// Used to notify about upcoming rotation
        /// </summary>
        Task<List<AgentApiKey>> GetKeysNearExpirationAsync(int daysThreshold = 10);

        /// <summary>
        /// Deactivate an API key
        /// </summary>
        Task DeactivateKeyAsync(Guid keyId);

        /// <summary>
        /// Delete all expired keys (cleanup)
        /// </summary>
        Task<int> DeleteExpiredKeysAsync();
    }
}
