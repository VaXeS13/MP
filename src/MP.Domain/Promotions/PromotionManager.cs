using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MP.Promotions;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using MP.Domain.Carts;

namespace MP.Domain.Promotions
{
    /// <summary>
    /// Domain service for managing promotion business logic
    /// </summary>
    public class PromotionManager : DomainService
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly IPromotionUsageRepository _promotionUsageRepository;

        public PromotionManager(
            IPromotionRepository promotionRepository,
            IPromotionUsageRepository promotionUsageRepository)
        {
            _promotionRepository = promotionRepository;
            _promotionUsageRepository = promotionUsageRepository;
        }

        /// <summary>
        /// Create a new promotion
        /// </summary>
        public async Task<Promotion> CreateAsync(
            string name,
            PromotionType type,
            PromotionDisplayMode displayMode,
            DiscountType discountType,
            decimal discountValue,
            string? promoCode = null)
        {
            // Validate promo code uniqueness if provided
            if (!string.IsNullOrWhiteSpace(promoCode))
            {
                var codeExists = await _promotionRepository.PromoCodeExistsAsync(promoCode);
                if (codeExists)
                    throw new BusinessException("PROMOTION_CODE_ALREADY_EXISTS")
                        .WithData("PromoCode", promoCode);
            }

            var promotion = new Promotion(
                GuidGenerator.Create(),
                name,
                type,
                displayMode,
                discountType,
                discountValue,
                CurrentTenant.Id);

            if (!string.IsNullOrWhiteSpace(promoCode))
            {
                promotion.SetPromoCode(promoCode);
            }

            return await _promotionRepository.InsertAsync(promotion);
        }

        /// <summary>
        /// Validate and apply promotion to cart
        /// </summary>
        public async Task<Promotion> ValidateAndApplyToCartAsync(
            Cart cart,
            string? promoCode = null)
        {
            Promotion? promotion = null;

            // If promo code provided, find promotion by code
            if (!string.IsNullOrWhiteSpace(promoCode))
            {
                promotion = await _promotionRepository.GetByPromoCodeAsync(promoCode);
                if (promotion == null)
                    throw new BusinessException("PROMOTION_CODE_NOT_FOUND")
                        .WithData("PromoCode", promoCode);
            }
            else
            {
                // Find best applicable automatic promotion
                promotion = await FindBestAutomaticPromotionAsync(cart);
                if (promotion == null)
                    throw new BusinessException("NO_APPLICABLE_PROMOTION_FOUND");
            }

            // Validate promotion
            await ValidatePromotionForCartAsync(promotion, cart);

            // Calculate discount
            var totalAmount = cart.GetTotalAmount();
            var discountAmount = promotion.CalculateDiscount(totalAmount);

            // Apply to cart
            cart.ApplyPromotion(promotion.Id, discountAmount, promoCode);

            return promotion;
        }

        /// <summary>
        /// Find best automatic promotion for cart (no promo code required)
        /// </summary>
        private async Task<Promotion?> FindBestAutomaticPromotionAsync(Cart cart)
        {
            var activePromotions = await _promotionRepository.GetActivePromotionsAsync();

            // Filter to automatic promotions (no promo code required)
            var automaticPromotions = activePromotions
                .Where(p => !p.RequiresPromoCode)
                .ToList();

            // Find applicable promotions
            var applicablePromotions = new List<(Promotion Promotion, decimal Discount)>();

            foreach (var promotion in automaticPromotions)
            {
                try
                {
                    await ValidatePromotionForCartAsync(promotion, cart, throwException: false);
                    var discount = promotion.CalculateDiscount(cart.GetTotalAmount());
                    if (discount > 0)
                    {
                        applicablePromotions.Add((promotion, discount));
                    }
                }
                catch
                {
                    // Skip invalid promotions
                    continue;
                }
            }

            // Return promotion with highest discount
            return applicablePromotions
                .OrderByDescending(p => p.Discount)
                .ThenByDescending(p => p.Promotion.Priority)
                .FirstOrDefault()
                .Promotion;
        }

        /// <summary>
        /// Validate if promotion can be applied to cart
        /// </summary>
        private async Task ValidatePromotionForCartAsync(
            Promotion promotion,
            Cart cart,
            bool throwException = true)
        {
            // Check if promotion is valid (active, within dates, usage limits)
            if (!promotion.IsValid())
            {
                if (throwException)
                    throw new BusinessException("PROMOTION_NOT_VALID");
                else
                    throw new Exception();
            }

            // Check minimum booths count
            if (promotion.MinimumBoothsCount.HasValue)
            {
                var boothCount = cart.GetItemCount();
                if (boothCount < promotion.MinimumBoothsCount.Value)
                {
                    if (throwException)
                        throw new BusinessException("PROMOTION_MINIMUM_BOOTHS_NOT_MET")
                            .WithData("Required", promotion.MinimumBoothsCount.Value)
                            .WithData("Current", boothCount);
                    else
                        throw new Exception();
                }
            }

            // Check booth type restrictions
            if (promotion.ApplicableBoothTypeIds.Any())
            {
                var cartBoothTypes = cart.Items.Select(i => i.BoothTypeId).Distinct().ToList();
                var hasApplicableBooth = cartBoothTypes.Any(bt => promotion.IsApplicableToBoothType(bt));

                if (!hasApplicableBooth)
                {
                    if (throwException)
                        throw new BusinessException("PROMOTION_NOT_APPLICABLE_TO_BOOTH_TYPES");
                    else
                        throw new Exception();
                }
            }

            // Check per-user usage limit
            if (promotion.MaxUsagePerUser.HasValue)
            {
                var userUsageCount = await _promotionUsageRepository.GetUsageCountByUserAsync(
                    promotion.Id,
                    cart.UserId);

                if (userUsageCount >= promotion.MaxUsagePerUser.Value)
                {
                    if (throwException)
                        throw new BusinessException("PROMOTION_USER_LIMIT_EXCEEDED")
                            .WithData("MaxUsage", promotion.MaxUsagePerUser.Value);
                    else
                        throw new Exception();
                }
            }
        }

        /// <summary>
        /// Record promotion usage after successful checkout
        /// </summary>
        public async Task<PromotionUsage> RecordUsageAsync(
            Guid promotionId,
            Guid userId,
            Guid cartId,
            decimal discountAmount,
            decimal originalAmount,
            decimal finalAmount,
            string? promoCodeUsed = null,
            Guid? rentalId = null)
        {
            var promotion = await _promotionRepository.GetAsync(promotionId);

            // Increment promotion usage count
            promotion.IncrementUsageCount();
            await _promotionRepository.UpdateAsync(promotion);

            // Create usage record
            var usage = new PromotionUsage(
                GuidGenerator.Create(),
                promotionId,
                userId,
                cartId,
                discountAmount,
                originalAmount,
                finalAmount,
                promoCodeUsed,
                rentalId,
                CurrentTenant.Id);

            return await _promotionUsageRepository.InsertAsync(usage);
        }

        /// <summary>
        /// Get all active promotions for display
        /// </summary>
        public async Task<List<Promotion>> GetActivePromotionsForDisplayAsync()
        {
            var promotions = await _promotionRepository.GetActivePromotionsAsync();

            // Filter to promotions with display mode (not None)
            return promotions
                .Where(p => p.DisplayMode != PromotionDisplayMode.None)
                .OrderByDescending(p => p.Priority)
                .ThenBy(p => p.CreationTime)
                .ToList();
        }

        /// <summary>
        /// Validate promo code and return promotion
        /// </summary>
        public async Task<Promotion> ValidatePromoCodeAsync(string promoCode)
        {
            if (string.IsNullOrWhiteSpace(promoCode))
                throw new BusinessException("PROMOTION_CODE_REQUIRED");

            var promotion = await _promotionRepository.GetByPromoCodeAsync(promoCode);
            if (promotion == null)
                throw new BusinessException("PROMOTION_CODE_NOT_FOUND")
                    .WithData("PromoCode", promoCode);

            if (!promotion.IsValid())
                throw new BusinessException("PROMOTION_CODE_EXPIRED_OR_INACTIVE");

            return promotion;
        }
    }
}
