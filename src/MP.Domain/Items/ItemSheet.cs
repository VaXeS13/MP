using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using MP.Domain.Rentals;

namespace MP.Domain.Items
{
    public class ItemSheet : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; private set; }
        public Guid UserId { get; private set; }
        public Guid? RentalId { get; private set; }
        public ItemSheetStatus Status { get; private set; }

        // Navigation properties
        public IdentityUser User { get; set; } = null!;
        public Rental? Rental { get; set; }

        private readonly List<ItemSheetItem> _items = new();
        public IReadOnlyList<ItemSheetItem> Items => _items.AsReadOnly();

        private ItemSheet() { }

        public ItemSheet(
            Guid id,
            Guid userId,
            Guid? tenantId = null) : base(id)
        {
            TenantId = tenantId;
            UserId = userId;
            Status = ItemSheetStatus.Draft;
        }

        public void AddItem(
            Guid itemId,
            decimal commissionPercentage = 0)
        {
            if (Status != ItemSheetStatus.Draft)
                throw new BusinessException("CAN_ONLY_ADD_ITEMS_TO_DRAFT_SHEET");

            if (_items.Any(x => x.ItemId == itemId))
                throw new BusinessException("ITEM_ALREADY_IN_SHEET");

            var itemNumber = _items.Count + 1;

            var sheetItem = new ItemSheetItem(
                Guid.NewGuid(),
                Id,
                itemId,
                itemNumber,
                commissionPercentage
            );

            _items.Add(sheetItem);
        }

        public void RemoveItem(Guid itemId)
        {
            if (Status != ItemSheetStatus.Draft)
                throw new BusinessException("CAN_ONLY_REMOVE_ITEMS_FROM_DRAFT_SHEET");

            var item = _items.FirstOrDefault(x => x.ItemId == itemId);
            if (item == null)
                throw new BusinessException("ITEM_NOT_FOUND_IN_SHEET");

            _items.Remove(item);

            // Renumber remaining items
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].SetItemNumber(i + 1);
            }
        }

        public void AssignToRental(Rental rental)
        {
            if (Status != ItemSheetStatus.Draft)
                throw new BusinessException("SHEET_ALREADY_ASSIGNED");

            if (rental.UserId != UserId)
                throw new BusinessException("SHEET_AND_RENTAL_MUST_BELONG_TO_SAME_USER");

            if (!rental.IsActive())
                throw new BusinessException("RENTAL_MUST_BE_ACTIVE");

            if (rental.Period.EndDate < DateTime.Today)
                throw new BusinessException("RENTAL_PERIOD_ALREADY_ENDED");

            if (_items.Count == 0)
                throw new BusinessException("CANNOT_ASSIGN_EMPTY_SHEET");

            RentalId = rental.Id;
            Status = ItemSheetStatus.Assigned;
        }

        public void UnassignFromRental()
        {
            if (Status == ItemSheetStatus.Ready)
                throw new BusinessException("CANNOT_UNASSIGN_READY_SHEET");

            if (Status != ItemSheetStatus.Assigned)
                throw new BusinessException("SHEET_NOT_ASSIGNED");

            RentalId = null;
            Status = ItemSheetStatus.Draft;
        }

        public void GenerateBarcodes()
        {
            if (Status != ItemSheetStatus.Assigned)
                throw new BusinessException("SHEET_MUST_BE_ASSIGNED_TO_GENERATE_BARCODES");

            if (_items.Count == 0)
                throw new BusinessException("CANNOT_GENERATE_BARCODES_FOR_EMPTY_SHEET");

            foreach (var item in _items)
            {
                if (string.IsNullOrEmpty(item.Barcode))
                {
                    item.GenerateBarcode();
                }
            }

            Status = ItemSheetStatus.Ready;
        }

        public ItemSheetItem? FindItemByBarcode(string barcode)
        {
            return _items.FirstOrDefault(x => x.Barcode == barcode);
        }

        public int GetItemsCount()
        {
            return _items.Count;
        }

        public int GetSoldItemsCount()
        {
            return _items.Count(x => x.Status == ItemSheetItemStatus.Sold);
        }

        public int GetReclaimedItemsCount()
        {
            return _items.Count(x => x.Status == ItemSheetItemStatus.Reclaimed);
        }

        public bool IsAllItemsSoldOrReclaimed()
        {
            return _items.All(x => x.Status == ItemSheetItemStatus.Sold ||
                                   x.Status == ItemSheetItemStatus.Reclaimed);
        }
    }
}
