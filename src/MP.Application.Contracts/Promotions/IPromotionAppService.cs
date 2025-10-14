using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MP.Promotions
{
    public interface IPromotionAppService : IApplicationService
    {
        /// <summary>
        /// Get list of promotions with filtering and pagination
        /// </summary>
        Task<PagedResultDto<PromotionDto>> GetListAsync(GetPromotionsInput input);

        /// <summary>
        /// Get promotion by ID
        /// </summary>
        Task<PromotionDto> GetAsync(Guid id);

        /// <summary>
        /// Create new promotion
        /// </summary>
        Task<PromotionDto> CreateAsync(CreatePromotionDto input);

        /// <summary>
        /// Update existing promotion
        /// </summary>
        Task<PromotionDto> UpdateAsync(Guid id, UpdatePromotionDto input);

        /// <summary>
        /// Delete promotion
        /// </summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Activate promotion
        /// </summary>
        Task<PromotionDto> ActivateAsync(Guid id);

        /// <summary>
        /// Deactivate promotion
        /// </summary>
        Task<PromotionDto> DeactivateAsync(Guid id);

        /// <summary>
        /// Get active promotions for display to customers
        /// </summary>
        Task<List<PromotionDto>> GetActivePromotionsAsync();

        /// <summary>
        /// Validate promo code and return promotion details
        /// </summary>
        Task<PromotionDto> ValidatePromoCodeAsync(ValidatePromoCodeInput input);

        /// <summary>
        /// Calculate discount for given amount
        /// </summary>
        Task<CalculateDiscountOutput> CalculateDiscountAsync(CalculateDiscountInput input);

        /// <summary>
        /// Apply promotion to current user's cart
        /// </summary>
        Task ApplyPromotionToCartAsync(ApplyPromotionToCartInput input);

        /// <summary>
        /// Remove promotion from current user's cart
        /// </summary>
        Task RemovePromotionFromCartAsync();
    }
}
