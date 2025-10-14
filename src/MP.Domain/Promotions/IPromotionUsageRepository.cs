using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.Promotions
{
    public interface IPromotionUsageRepository : IRepository<PromotionUsage, Guid>
    {
        /// <summary>
        /// Get usage count for specific promotion by user
        /// </summary>
        Task<int> GetUsageCountByUserAsync(
            Guid promotionId,
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all usages for a specific promotion
        /// </summary>
        Task<List<PromotionUsage>> GetByPromotionIdAsync(
            Guid promotionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get usage history for a user
        /// </summary>
        Task<List<PromotionUsage>> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if user has already used promotion
        /// </summary>
        Task<bool> HasUserUsedPromotionAsync(
            Guid promotionId,
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}
