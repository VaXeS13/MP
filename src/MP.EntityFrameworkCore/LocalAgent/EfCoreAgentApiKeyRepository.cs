using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MP.Domain.LocalAgent;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace MP.EntityFrameworkCore.LocalAgent
{
    /// <summary>
    /// EF Core implementation of IAgentApiKeyRepository
    /// Handles database operations for agent API keys
    /// </summary>
    public class EfCoreAgentApiKeyRepository : EfCoreRepository<MPDbContext, AgentApiKey, Guid>, IAgentApiKeyRepository
    {
        public EfCoreAgentApiKeyRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        /// <summary>
        /// Find an API key by its hash for authentication
        /// </summary>
        public async Task<AgentApiKey?> FindByKeyHashAsync(string keyHash, Guid tenantId)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.AgentApiKeys
                .AsNoTracking()
                .Where(k => k.KeyHash == keyHash && k.TenantId == tenantId)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Get all active API keys for a specific agent
        /// </summary>
        public async Task<List<AgentApiKey>> GetActiveKeysForAgentAsync(string agentId, Guid tenantId)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.AgentApiKeys
                .AsNoTracking()
                .Where(k => k.AgentId == agentId &&
                           k.TenantId == tenantId &&
                           k.IsActive &&
                           k.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(k => k.CreationTime)
                .ToListAsync();
        }

        /// <summary>
        /// Get all API keys for a tenant
        /// </summary>
        public async Task<List<AgentApiKey>> GetAllKeysForTenantAsync(Guid tenantId)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.AgentApiKeys
                .AsNoTracking()
                .Where(k => k.TenantId == tenantId)
                .OrderByDescending(k => k.CreationTime)
                .ToListAsync();
        }

        /// <summary>
        /// Find an API key by agent ID and suffix (last 8 characters)
        /// Useful for identifying and rotating keys
        /// </summary>
        public async Task<AgentApiKey?> FindByAgentAndSuffixAsync(string agentId, string suffix, Guid tenantId)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.AgentApiKeys
                .AsNoTracking()
                .Where(k => k.AgentId == agentId &&
                           k.Suffix == suffix &&
                           k.TenantId == tenantId)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Get all expired API keys for cleanup
        /// </summary>
        public async Task<List<AgentApiKey>> GetExpiredKeysAsync()
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.AgentApiKeys
                .AsNoTracking()
                .Where(k => k.ExpiresAt <= DateTime.UtcNow)
                .OrderBy(k => k.ExpiresAt)
                .ToListAsync();
        }

        /// <summary>
        /// Get all keys that are approaching expiration
        /// Used to trigger rotation reminders
        /// </summary>
        public async Task<List<AgentApiKey>> GetKeysNearExpirationAsync(int daysThreshold = 10)
        {
            var dbContext = await GetDbContextAsync();
            var thresholdDate = DateTime.UtcNow.AddDays(daysThreshold);

            return await dbContext.AgentApiKeys
                .AsNoTracking()
                .Where(k => k.IsActive &&
                           k.ExpiresAt <= thresholdDate &&
                           k.ExpiresAt > DateTime.UtcNow &&
                           !k.ShouldRotate)
                .OrderBy(k => k.ExpiresAt)
                .ToListAsync();
        }

        /// <summary>
        /// Deactivate an API key
        /// </summary>
        public async Task DeactivateKeyAsync(Guid keyId)
        {
            var dbContext = await GetDbContextAsync();
            var key = await dbContext.AgentApiKeys.FindAsync(keyId);

            if (key != null)
            {
                key.IsActive = false;
                await dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Delete all expired API keys
        /// This should be run periodically for cleanup
        /// </summary>
        public async Task<int> DeleteExpiredKeysAsync()
        {
            var dbContext = await GetDbContextAsync();
            var expiredKeys = await dbContext.AgentApiKeys
                .Where(k => k.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync();

            if (expiredKeys.Count == 0)
                return 0;

            dbContext.AgentApiKeys.RemoveRange(expiredKeys);
            await dbContext.SaveChangesAsync();

            return expiredKeys.Count;
        }

        /// <summary>
        /// Get all keys that are currently locked due to failed attempts
        /// </summary>
        public async Task<List<AgentApiKey>> GetLockedKeysAsync()
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.AgentApiKeys
                .AsNoTracking()
                .Where(k => k.LockedUntil.HasValue && k.LockedUntil > DateTime.UtcNow)
                .ToListAsync();
        }

        /// <summary>
        /// Get unused API keys (not used within specified days)
        /// Useful for security audits
        /// </summary>
        public async Task<List<AgentApiKey>> GetUnusedKeysAsync(int daysThreshold = 30)
        {
            var dbContext = await GetDbContextAsync();
            var thresholdDate = DateTime.UtcNow.AddDays(-daysThreshold);

            return await dbContext.AgentApiKeys
                .AsNoTracking()
                .Where(k => (k.LastUsedAt == null || k.LastUsedAt < thresholdDate) && k.IsActive)
                .OrderBy(k => k.LastUsedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Get most frequently used API keys
        /// Useful for monitoring
        /// </summary>
        public async Task<List<AgentApiKey>> GetMostUsedKeysAsync(int count = 10)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.AgentApiKeys
                .AsNoTracking()
                .Where(k => k.IsActive)
                .OrderByDescending(k => k.UsageCount)
                .Take(count)
                .ToListAsync();
        }

        /// <summary>
        /// Count active API keys per agent
        /// </summary>
        public async Task<Dictionary<string, int>> CountActiveKeysPerAgentAsync(Guid tenantId)
        {
            var dbContext = await GetDbContextAsync();
            var result = await dbContext.AgentApiKeys
                .AsNoTracking()
                .Where(k => k.TenantId == tenantId &&
                           k.IsActive &&
                           k.ExpiresAt > DateTime.UtcNow)
                .GroupBy(k => k.AgentId)
                .Select(g => new { AgentId = g.Key, Count = g.Count() })
                .ToListAsync();

            return result.ToDictionary(x => x.AgentId, x => x.Count);
        }
    }
}
