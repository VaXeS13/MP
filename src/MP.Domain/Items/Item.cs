using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using MP.Domain.Booths;

namespace MP.Domain.Items
{
    public class Item : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; private set; }
        public Guid UserId { get; private set; }
        public string Name { get; private set; } = null!;
        public string? Category { get; private set; }
        public decimal Price { get; private set; }
        public Currency Currency { get; private set; }
        public ItemStatus Status { get; private set; }

        // Navigation property
        public IdentityUser User { get; set; } = null!;

        private Item() { }

        public Item(
            Guid id,
            Guid userId,
            string name,
            decimal price,
            Currency currency = Currency.PLN,
            Guid? tenantId = null) : base(id)
        {
            TenantId = tenantId;
            UserId = userId;
            SetName(name);
            SetPrice(price);
            Currency = currency;
            Status = ItemStatus.Draft;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new BusinessException("ITEM_NAME_REQUIRED");

            if (name.Length > 200)
                throw new BusinessException("ITEM_NAME_TOO_LONG");

            Name = name.Trim();
        }

        public void SetCategory(string? category)
        {
            if (category != null && category.Length > 100)
                throw new BusinessException("ITEM_CATEGORY_TOO_LONG");

            Category = category?.Trim();
        }

        public void SetPrice(decimal price)
        {
            if (price <= 0)
                throw new BusinessException("ITEM_PRICE_MUST_BE_POSITIVE");

            Price = price;
        }

        public void SetCurrency(Currency currency)
        {
            Currency = currency;
        }

        public void MarkAsInSheet()
        {
            if (Status != ItemStatus.Draft)
                throw new BusinessException("ONLY_DRAFT_ITEMS_CAN_BE_ADDED_TO_SHEET");

            Status = ItemStatus.InSheet;
        }

        public void MarkAsForSale()
        {
            if (Status != ItemStatus.InSheet)
                throw new BusinessException("ONLY_ITEMS_IN_SHEET_CAN_BE_MARKED_FOR_SALE");

            Status = ItemStatus.ForSale;
        }

        public void MarkAsSold()
        {
            if (Status != ItemStatus.ForSale)
                throw new BusinessException("ONLY_FOR_SALE_ITEMS_CAN_BE_SOLD");

            Status = ItemStatus.Sold;
        }

        public void MarkAsReclaimed()
        {
            if (Status == ItemStatus.Sold)
                throw new BusinessException("CANNOT_RECLAIM_SOLD_ITEM");

            Status = ItemStatus.Reclaimed;
        }

        public void MarkAsDraft()
        {
            if (Status == ItemStatus.Sold)
                throw new BusinessException("CANNOT_REVERT_SOLD_ITEM_TO_DRAFT");

            Status = ItemStatus.Draft;
        }
    }
}
