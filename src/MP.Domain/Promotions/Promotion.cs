using System;
using System.Collections.Generic;
using System.Linq;
using MP.Promotions;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.Promotions
{
    /// <summary>
    /// Represents a promotional offer that can be applied to booth rentals
    /// </summary>
    public class Promotion : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; private set; }

        /// <summary>
        /// Promotion name (internal use)
        /// </summary>
        public string Name { get; private set; } = null!;

        /// <summary>
        /// Optional description for admin reference
        /// </summary>
        public string? Description { get; private set; }

        /// <summary>
        /// Type of promotion (Quantity, PromoCode, DateRange)
        /// </summary>
        public PromotionType Type { get; private set; }

        /// <summary>
        /// How to display promotion notification to customers
        /// </summary>
        public PromotionDisplayMode DisplayMode { get; private set; }

        /// <summary>
        /// Whether promotion is currently active
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Promotion validity start date (null = no start restriction)
        /// </summary>
        public DateTime? ValidFrom { get; private set; }

        /// <summary>
        /// Promotion validity end date (null = no end restriction)
        /// </summary>
        public DateTime? ValidTo { get; private set; }

        /// <summary>
        /// Priority for display (higher = shown first)
        /// </summary>
        public int Priority { get; private set; }

        // Promotion conditions

        /// <summary>
        /// Minimum number of booths required to activate promotion
        /// </summary>
        public int? MinimumBoothsCount { get; private set; }

        /// <summary>
        /// Promo code that user must enter (for PromoCode type)
        /// </summary>
        public string? PromoCode { get; private set; }

        /// <summary>
        /// Whether promo code is required to activate this promotion
        /// </summary>
        public bool RequiresPromoCode { get; private set; }

        // Discount configuration

        /// <summary>
        /// Type of discount (Percentage or FixedAmount)
        /// </summary>
        public DiscountType DiscountType { get; private set; }

        /// <summary>
        /// Discount value (percentage 0-100 or fixed amount in currency)
        /// </summary>
        public decimal DiscountValue { get; private set; }

        /// <summary>
        /// Maximum discount amount (for percentage discounts)
        /// </summary>
        public decimal? MaxDiscountAmount { get; private set; }

        // Usage limits

        /// <summary>
        /// Maximum total uses of this promotion (null = unlimited)
        /// </summary>
        public int? MaxUsageCount { get; private set; }

        /// <summary>
        /// Current number of times promotion has been used
        /// </summary>
        public int CurrentUsageCount { get; private set; }

        /// <summary>
        /// Maximum uses per user (null = unlimited)
        /// </summary>
        public int? MaxUsagePerUser { get; private set; }

        // Customer-facing message

        /// <summary>
        /// Message displayed to customers
        /// </summary>
        public string? CustomerMessage { get; private set; }

        // Applicable booth types

        private readonly List<Guid> _applicableBoothTypeIds = new();

        /// <summary>
        /// Booth type IDs this promotion applies to (empty = all types)
        /// </summary>
        public IReadOnlyList<Guid> ApplicableBoothTypeIds => _applicableBoothTypeIds.AsReadOnly();

        // Constructor for EF Core
        private Promotion() { }

        public Promotion(
            Guid id,
            string name,
            PromotionType type,
            PromotionDisplayMode displayMode,
            DiscountType discountType,
            decimal discountValue,
            Guid? tenantId = null) : base(id)
        {
            TenantId = tenantId;
            SetName(name);
            Type = type;
            DisplayMode = displayMode;
            SetDiscount(discountType, discountValue);
            IsActive = false;
            Priority = 0;
            CurrentUsageCount = 0;
            RequiresPromoCode = type == PromotionType.PromoCode;
        }

        // Setters with validation

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new BusinessException("PROMOTION_NAME_REQUIRED");

            if (name.Length > 200)
                throw new BusinessException("PROMOTION_NAME_TOO_LONG");

            Name = name.Trim();
        }

        public void SetDescription(string? description)
        {
            if (description != null && description.Length > 1000)
                throw new BusinessException("PROMOTION_DESCRIPTION_TOO_LONG");

            Description = description?.Trim();
        }

        public void SetType(PromotionType type)
        {
            Type = type;
            RequiresPromoCode = type == PromotionType.PromoCode;
        }

        public void SetDisplayMode(PromotionDisplayMode displayMode)
        {
            DisplayMode = displayMode;
        }

        public void SetDiscount(DiscountType discountType, decimal discountValue)
        {
            if (discountValue <= 0)
                throw new BusinessException("PROMOTION_DISCOUNT_MUST_BE_POSITIVE");

            if (discountType == DiscountType.Percentage && discountValue > 100)
                throw new BusinessException("PROMOTION_PERCENTAGE_CANNOT_EXCEED_100");

            DiscountType = discountType;
            DiscountValue = discountValue;
        }

        public void SetMaxDiscountAmount(decimal? maxAmount)
        {
            if (maxAmount.HasValue && maxAmount.Value <= 0)
                throw new BusinessException("PROMOTION_MAX_DISCOUNT_MUST_BE_POSITIVE");

            MaxDiscountAmount = maxAmount;
        }

        public void SetValidityPeriod(DateTime? validFrom, DateTime? validTo)
        {
            if (validFrom.HasValue && validTo.HasValue && validFrom.Value >= validTo.Value)
                throw new BusinessException("PROMOTION_INVALID_VALIDITY_PERIOD");

            ValidFrom = validFrom;
            ValidTo = validTo;
        }

        public void SetPriority(int priority)
        {
            if (priority < 0)
                throw new BusinessException("PROMOTION_PRIORITY_CANNOT_BE_NEGATIVE");

            Priority = priority;
        }

        public void SetMinimumBoothsCount(int? count)
        {
            if (count.HasValue && count.Value <= 0)
                throw new BusinessException("PROMOTION_MINIMUM_BOOTHS_MUST_BE_POSITIVE");

            MinimumBoothsCount = count;
        }

        public void SetPromoCode(string? code)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                if (code.Length > 50)
                    throw new BusinessException("PROMOTION_CODE_TOO_LONG");

                PromoCode = code.Trim().ToUpperInvariant();
            }
            else
            {
                PromoCode = null;
            }
        }

        public void SetUsageLimits(int? maxUsageCount, int? maxUsagePerUser)
        {
            if (maxUsageCount.HasValue && maxUsageCount.Value <= 0)
                throw new BusinessException("PROMOTION_MAX_USAGE_MUST_BE_POSITIVE");

            if (maxUsagePerUser.HasValue && maxUsagePerUser.Value <= 0)
                throw new BusinessException("PROMOTION_MAX_USAGE_PER_USER_MUST_BE_POSITIVE");

            MaxUsageCount = maxUsageCount;
            MaxUsagePerUser = maxUsagePerUser;
        }

        public void SetCustomerMessage(string? message)
        {
            if (message != null && message.Length > 500)
                throw new BusinessException("PROMOTION_CUSTOMER_MESSAGE_TOO_LONG");

            CustomerMessage = message?.Trim();
        }

        public void SetApplicableBoothTypes(List<Guid> boothTypeIds)
        {
            _applicableBoothTypeIds.Clear();
            if (boothTypeIds != null && boothTypeIds.Any())
            {
                _applicableBoothTypeIds.AddRange(boothTypeIds.Distinct());
            }
        }

        // Activation/Deactivation

        public void Activate()
        {
            if (IsActive)
                throw new BusinessException("PROMOTION_ALREADY_ACTIVE");

            if (Type == PromotionType.PromoCode && string.IsNullOrWhiteSpace(PromoCode))
                throw new BusinessException("PROMOTION_CODE_REQUIRED_FOR_PROMO_CODE_TYPE");

            IsActive = true;
        }

        public void Deactivate()
        {
            if (!IsActive)
                throw new BusinessException("PROMOTION_ALREADY_INACTIVE");

            IsActive = false;
        }

        // Usage tracking

        public void IncrementUsageCount()
        {
            CurrentUsageCount++;
        }

        // Validation methods

        public bool IsValid()
        {
            if (!IsActive)
                return false;

            var now = DateTime.UtcNow;

            if (ValidFrom.HasValue && now < ValidFrom.Value)
                return false;

            if (ValidTo.HasValue && now > ValidTo.Value)
                return false;

            if (MaxUsageCount.HasValue && CurrentUsageCount >= MaxUsageCount.Value)
                return false;

            return true;
        }

        public bool IsApplicableToBoothType(Guid boothTypeId)
        {
            // If no specific booth types defined, applies to all
            if (!_applicableBoothTypeIds.Any())
                return true;

            return _applicableBoothTypeIds.Contains(boothTypeId);
        }

        public decimal CalculateDiscount(decimal totalAmount)
        {
            if (!IsValid())
                return 0;

            decimal discount = DiscountType == DiscountType.Percentage
                ? totalAmount * (DiscountValue / 100m)
                : DiscountValue;

            // Apply max discount limit for percentage discounts
            if (DiscountType == DiscountType.Percentage
                && MaxDiscountAmount.HasValue
                && discount > MaxDiscountAmount.Value)
            {
                discount = MaxDiscountAmount.Value;
            }

            // Discount cannot exceed total amount
            return Math.Min(discount, totalAmount);
        }
    }
}
