using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MP.Promotions;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.Promotions
{
    public interface IPromotionRepository : IRepository<Promotion, Guid>
    {
        /// <summary>
        /// Get active promotions for display to customers
        /// </summary>
        Task<List<Promotion>> GetActivePromotionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get promotion by promo code
        /// </summary>
        Task<Promotion?> GetByPromoCodeAsync(string promoCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get promotions with filtering and sorting
        /// </summary>
        Task<List<Promotion>> GetListAsync(
            string? filterText = null,
            bool? isActive = null,
            PromotionType? type = null,
            int skipCount = 0,
            int maxResultCount = int.MaxValue,
            string sorting = "Priority DESC, CreationTime DESC",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get total count with filtering
        /// </summary>
        Task<long> GetCountAsync(
            string? filterText = null,
            bool? isActive = null,
            PromotionType? type = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if promotion code exists (case-insensitive)
        /// </summary>
        Task<bool> PromoCodeExistsAsync(
            string promoCode,
            Guid? excludeId = null,
            CancellationToken cancellationToken = default);
    }
}
