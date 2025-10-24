using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.Promotions
{
    /// <summary>
    /// Tracks individual uses of promotions by users
    /// Used for enforcing per-user usage limits and analytics
    /// </summary>
    public class PromotionUsage : CreationAuditedEntity<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; private set; }
        public Guid OrganizationalUnitId { get; private set; }

        /// <summary>
        /// The promotion that was used
        /// </summary>
        public Guid PromotionId { get; private set; }

        /// <summary>
        /// User who used the promotion
        /// </summary>
        public Guid UserId { get; private set; }

        /// <summary>
        /// Cart where promotion was applied
        /// </summary>
        public Guid CartId { get; private set; }

        /// <summary>
        /// Rental created from cart (null if cart not yet checked out)
        /// </summary>
        public Guid? RentalId { get; private set; }

        /// <summary>
        /// Actual discount amount applied
        /// </summary>
        public decimal DiscountAmount { get; private set; }

        /// <summary>
        /// Promo code that was used (if applicable)
        /// </summary>
        public string? PromoCodeUsed { get; private set; }

        /// <summary>
        /// Original cart total before discount
        /// </summary>
        public decimal OriginalAmount { get; private set; }

        /// <summary>
        /// Final cart total after discount
        /// </summary>
        public decimal FinalAmount { get; private set; }

        // Navigation property
        public Promotion Promotion { get; set; } = null!;

        // Constructor for EF Core
        private PromotionUsage() { }

        public PromotionUsage(
            Guid id,
            Guid organizationalUnitId,
            Guid promotionId,
            Guid userId,
            Guid cartId,
            decimal discountAmount,
            decimal originalAmount,
            decimal finalAmount,
            string? promoCodeUsed = null,
            Guid? rentalId = null,
            Guid? tenantId = null) : base(id)
        {
            TenantId = tenantId;
            OrganizationalUnitId = organizationalUnitId;
            PromotionId = promotionId;
            UserId = userId;
            CartId = cartId;
            RentalId = rentalId;
            DiscountAmount = discountAmount;
            PromoCodeUsed = promoCodeUsed;
            OriginalAmount = originalAmount;
            FinalAmount = finalAmount;
        }

        public void SetRentalId(Guid rentalId)
        {
            RentalId = rentalId;
        }
    }
}
