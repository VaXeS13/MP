using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace MP.Domain.Items
{
    public class ItemSheetItem : FullAuditedEntity<Guid>
    {
        public Guid ItemSheetId { get; private set; }
        public Guid ItemId { get; private set; }
        public int ItemNumber { get; private set; }
        public string? Barcode { get; private set; }
        public decimal CommissionPercentage { get; private set; }
        public ItemSheetItemStatus Status { get; private set; }
        public DateTime? SoldAt { get; private set; }

        // Navigation properties
        public ItemSheet ItemSheet { get; set; } = null!;
        public Item Item { get; set; } = null!;

        private ItemSheetItem() { }

        public ItemSheetItem(
            Guid id,
            Guid itemSheetId,
            Guid itemId,
            int itemNumber,
            decimal commissionPercentage) : base(id)
        {
            ItemSheetId = itemSheetId;
            ItemId = itemId;
            SetItemNumber(itemNumber);
            SetCommissionPercentage(commissionPercentage);
            Status = ItemSheetItemStatus.ForSale;
        }

        public void SetItemNumber(int itemNumber)
        {
            if (itemNumber <= 0)
                throw new BusinessException("ITEM_NUMBER_MUST_BE_POSITIVE");

            ItemNumber = itemNumber;
        }

        public void SetCommissionPercentage(decimal commissionPercentage)
        {
            if (commissionPercentage < 0 || commissionPercentage > 100)
                throw new BusinessException("INVALID_COMMISSION_PERCENTAGE");

            CommissionPercentage = commissionPercentage;
        }

        public void GenerateBarcode()
        {
            if (!string.IsNullOrEmpty(Barcode))
                throw new BusinessException("BARCODE_ALREADY_GENERATED");

            Barcode = BarcodeHelper.GenerateBarcodeFromGuid(Id);
        }

        public void MarkAsSold(DateTime soldAt)
        {
            if (Status != ItemSheetItemStatus.ForSale)
                throw new BusinessException("ONLY_FOR_SALE_ITEMS_CAN_BE_SOLD");

            if (string.IsNullOrEmpty(Barcode))
                throw new BusinessException("ITEM_MUST_HAVE_BARCODE_BEFORE_SELLING");

            Status = ItemSheetItemStatus.Sold;
            SoldAt = soldAt;
        }

        public void MarkAsReclaimed()
        {
            if (Status == ItemSheetItemStatus.Sold)
                throw new BusinessException("CANNOT_RECLAIM_SOLD_ITEM");

            Status = ItemSheetItemStatus.Reclaimed;
        }

        public decimal GetCommissionAmount(decimal price)
        {
            if (Status != ItemSheetItemStatus.Sold)
                return 0;

            return price * (CommissionPercentage / 100);
        }

        public decimal GetCustomerAmount(decimal price)
        {
            if (Status != ItemSheetItemStatus.Sold)
                return 0;

            return price - GetCommissionAmount(price);
        }
    }
}
