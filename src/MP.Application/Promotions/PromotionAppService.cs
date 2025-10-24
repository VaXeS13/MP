using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MP.Carts;
using MP.Domain.Promotions;
using MP.Domain.Carts;
using MP.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MP.Promotions
{
    [Authorize(MPPermissions.Promotions.Default)]
    public class PromotionAppService : ApplicationService, IPromotionAppService
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly IPromotionUsageRepository _promotionUsageRepository;
        private readonly PromotionManager _promotionManager;
        private readonly IRepository<Cart, Guid> _cartRepository;

        public PromotionAppService(
            IPromotionRepository promotionRepository,
            IPromotionUsageRepository promotionUsageRepository,
            PromotionManager promotionManager,
            IRepository<Cart, Guid> cartRepository)
        {
            _promotionRepository = promotionRepository;
            _promotionUsageRepository = promotionUsageRepository;
            _promotionManager = promotionManager;
            _cartRepository = cartRepository;
        }

        public async Task<PagedResultDto<PromotionDto>> GetListAsync(GetPromotionsInput input)
        {
            var totalCount = await _promotionRepository.GetCountAsync(
                input.FilterText,
                input.IsActive,
                input.Type);

            var promotions = await _promotionRepository.GetListAsync(
                input.FilterText,
                input.IsActive,
                input.Type,
                input.SkipCount,
                input.MaxResultCount,
                input.Sorting);

            return new PagedResultDto<PromotionDto>(
                totalCount,
                ObjectMapper.Map<List<Promotion>, List<PromotionDto>>(promotions));
        }

        public async Task<PromotionDto> GetAsync(Guid id)
        {
            var promotion = await _promotionRepository.GetAsync(id);
            return ObjectMapper.Map<Promotion, PromotionDto>(promotion);
        }

        [Authorize(MPPermissions.Promotions.Create)]
        public async Task<PromotionDto> CreateAsync(CreatePromotionDto input)
        {
            var promotion = await _promotionManager.CreateAsync(
                input.Name,
                Guid.Empty, // TODO: Get organizationalUnitId from user context or input
                input.Type,
                input.DisplayMode,
                input.DiscountType,
                input.DiscountValue,
                input.PromoCode);

            // Set optional properties
            promotion.SetDescription(input.Description);
            promotion.SetValidityPeriod(input.ValidFrom, input.ValidTo);
            promotion.SetPriority(input.Priority);
            promotion.SetMinimumBoothsCount(input.MinimumBoothsCount);
            promotion.SetMaxDiscountAmount(input.MaxDiscountAmount);
            promotion.SetUsageLimits(input.MaxUsageCount, input.MaxUsagePerUser);
            promotion.SetCustomerMessage(input.CustomerMessage);
            promotion.SetMaxAccountAgeDays(input.MaxAccountAgeDays);
            promotion.SetApplicableBoothTypes(input.ApplicableBoothTypeIds);
            promotion.SetApplicableBooths(input.ApplicableBoothIds);

            // Set IsActive based on input
            if (input.IsActive)
            {
                promotion.Activate();
            }
            // Note: Promotion is created with IsActive=false by default,
            // so we don't need to call Deactivate() if input.IsActive is false

            var savedPromotion = await _promotionRepository.InsertAsync(promotion);

            return ObjectMapper.Map<Promotion, PromotionDto>(savedPromotion);
        }

        [Authorize(MPPermissions.Promotions.Edit)]
        public async Task<PromotionDto> UpdateAsync(Guid id, UpdatePromotionDto input)
        {
            var promotion = await _promotionRepository.GetAsync(id);

            // Update properties
            promotion.SetName(input.Name);
            promotion.SetDescription(input.Description);
            promotion.SetType(input.Type);
            promotion.SetDisplayMode(input.DisplayMode);
            promotion.SetDiscount(input.DiscountType, input.DiscountValue);
            promotion.SetMaxDiscountAmount(input.MaxDiscountAmount);
            promotion.SetValidityPeriod(input.ValidFrom, input.ValidTo);
            promotion.SetPriority(input.Priority);
            promotion.SetMinimumBoothsCount(input.MinimumBoothsCount);
            promotion.SetUsageLimits(input.MaxUsageCount, input.MaxUsagePerUser);
            promotion.SetCustomerMessage(input.CustomerMessage);
            promotion.SetMaxAccountAgeDays(input.MaxAccountAgeDays);
            promotion.SetApplicableBoothTypes(input.ApplicableBoothTypeIds);
            promotion.SetApplicableBooths(input.ApplicableBoothIds);

            // Update IsActive status
            if (input.IsActive && !promotion.IsActive)
            {
                promotion.Activate();
            }
            else if (!input.IsActive && promotion.IsActive)
            {
                promotion.Deactivate();
            }

            // Validate and update promo code if provided
            if (!string.IsNullOrWhiteSpace(input.PromoCode))
            {
                var codeExists = await _promotionRepository.PromoCodeExistsAsync(
                    input.PromoCode,
                    excludeId: id);

                if (codeExists)
                    throw new BusinessException("PROMOTION_CODE_ALREADY_EXISTS")
                        .WithData("PromoCode", input.PromoCode);

                promotion.SetPromoCode(input.PromoCode);
            }
            else
            {
                promotion.SetPromoCode(null);
            }

            var updatedPromotion = await _promotionRepository.UpdateAsync(promotion);

            return ObjectMapper.Map<Promotion, PromotionDto>(updatedPromotion);
        }

        [Authorize(MPPermissions.Promotions.Delete)]
        public async Task DeleteAsync(Guid id)
        {
            await _promotionRepository.DeleteAsync(id);
        }

        [Authorize(MPPermissions.Promotions.Manage)]
        public async Task<PromotionDto> ActivateAsync(Guid id)
        {
            var promotion = await _promotionRepository.GetAsync(id);
            promotion.Activate();
            var updatedPromotion = await _promotionRepository.UpdateAsync(promotion);

            return ObjectMapper.Map<Promotion, PromotionDto>(updatedPromotion);
        }

        [Authorize(MPPermissions.Promotions.Manage)]
        public async Task<PromotionDto> DeactivateAsync(Guid id)
        {
            var promotion = await _promotionRepository.GetAsync(id);
            promotion.Deactivate();
            var updatedPromotion = await _promotionRepository.UpdateAsync(promotion);

            return ObjectMapper.Map<Promotion, PromotionDto>(updatedPromotion);
        }

        [AllowAnonymous]
        public async Task<List<PromotionDto>> GetActivePromotionsAsync()
        {
            var promotions = await _promotionManager.GetActivePromotionsForDisplayAsync();
            return ObjectMapper.Map<List<Promotion>, List<PromotionDto>>(promotions);
        }

        [Authorize]
        public async Task<PromotionDto> ValidatePromoCodeAsync(ValidatePromoCodeInput input)
        {
            var promotion = await _promotionManager.ValidatePromoCodeAsync(input.PromoCode);
            return ObjectMapper.Map<Promotion, PromotionDto>(promotion);
        }

        public async Task<CalculateDiscountOutput> CalculateDiscountAsync(CalculateDiscountInput input)
        {
            var promotion = await _promotionRepository.GetAsync(input.PromotionId);
            var discountAmount = promotion.CalculateDiscount(input.TotalAmount);
            var finalAmount = input.TotalAmount - discountAmount;

            return new CalculateDiscountOutput
            {
                DiscountAmount = discountAmount,
                FinalAmount = finalAmount,
                PromotionName = promotion.Name
            };
        }

        [Authorize]
        public async Task ApplyPromotionToCartAsync(ApplyPromotionToCartInput input)
        {
            if (CurrentUser?.Id == null)
                throw new BusinessException("USER_NOT_AUTHENTICATED");

            // Get user's active cart with items
            var queryable = await _cartRepository.GetQueryableAsync();
            var cart = await queryable
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c =>
                    c.UserId == CurrentUser.Id.Value &&
                    c.Status == CartStatus.Active);

            if (cart == null)
                throw new BusinessException("CART_NOT_FOUND");

            // Validate and apply promotion
            var promotion = await _promotionManager.ValidateAndApplyToCartAsync(cart, input.PromoCode);

            // Save cart
            await _cartRepository.UpdateAsync(cart);
        }

        [Authorize]
        public async Task RemovePromotionFromCartAsync()
        {
            if (CurrentUser?.Id == null)
                throw new BusinessException("USER_NOT_AUTHENTICATED");

            // Get user's active cart with items
            var queryable = await _cartRepository.GetQueryableAsync();
            var cart = await queryable
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c =>
                    c.UserId == CurrentUser.Id.Value &&
                    c.Status == CartStatus.Active);

            if (cart == null)
                throw new BusinessException("CART_NOT_FOUND");

            cart.RemovePromotion();

            await _cartRepository.UpdateAsync(cart);
        }
    }
}
