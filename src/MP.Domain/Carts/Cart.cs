using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using MP.Carts;

namespace MP.Domain.Carts
{
    public class Cart : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; private set; }
        public Guid UserId { get; private set; }
        public CartStatus Status { get; private set; }
        public DateTime? ExtensionTimeoutAt { get; private set; }

        // Navigation properties
        public IdentityUser User { get; set; } = null!;

        private readonly List<CartItem> _items = new();
        public IReadOnlyList<CartItem> Items => _items.AsReadOnly();

        private Cart() { }

        public Cart(
            Guid id,
            Guid userId,
            Guid? tenantId = null) : base(id)
        {
            UserId = userId;
            TenantId = tenantId;
            Status = CartStatus.Active;
        }

        // Domain Methods

        public CartItem AddItem(
            Guid itemId,
            Guid boothId,
            Guid boothTypeId,
            DateTime startDate,
            DateTime endDate,
            decimal pricePerDay,
            string? notes = null)
        {
            if (Status != CartStatus.Active)
                throw new BusinessException("CART_NOT_ACTIVE");

            // Check if same booth already in cart with overlapping dates
            var existingItem = _items.FirstOrDefault(item =>
                item.BoothId == boothId &&
                item.OverlapsWith(startDate, endDate));

            if (existingItem != null)
                throw new BusinessException("CART_BOOTH_ALREADY_ADDED_WITH_OVERLAPPING_DATES")
                    .WithData("ExistingItemId", existingItem.Id)
                    .WithData("BoothId", boothId);

            var item = new CartItem(
                itemId,
                Id,
                boothId,
                boothTypeId,
                startDate,
                endDate,
                pricePerDay,
                CartItemType.Rental,
                null,
                notes
            );

            _items.Add(item);

            return item;
        }

        public void UpdateItem(
            Guid itemId,
            Guid boothTypeId,
            DateTime startDate,
            DateTime endDate,
            string? notes)
        {
            if (Status != CartStatus.Active)
                throw new BusinessException("CART_NOT_ACTIVE");

            var item = _items.FirstOrDefault(i => i.Id == itemId);
            if (item == null)
                throw new BusinessException("CART_ITEM_NOT_FOUND")
                    .WithData("ItemId", itemId);

            // Check for overlapping dates with other items for the same booth
            var overlappingItem = _items.FirstOrDefault(i =>
                i.Id != itemId &&
                i.BoothId == item.BoothId &&
                i.OverlapsWith(startDate, endDate));

            if (overlappingItem != null)
                throw new BusinessException("CART_ITEM_OVERLAPS_WITH_ANOTHER")
                    .WithData("ItemId", itemId)
                    .WithData("OverlappingItemId", overlappingItem.Id);

            item.UpdateBoothType(boothTypeId);
            item.SetPeriod(startDate, endDate);
            item.SetNotes(notes);
        }

        public void RemoveItem(Guid itemId)
        {
            if (Status != CartStatus.Active)
                throw new BusinessException("CART_NOT_ACTIVE");

            var item = _items.FirstOrDefault(i => i.Id == itemId);
            if (item == null)
                throw new BusinessException("CART_ITEM_NOT_FOUND")
                    .WithData("ItemId", itemId);

            _items.Remove(item);
        }

        public void Clear()
        {
            if (Status != CartStatus.Active)
                throw new BusinessException("CART_NOT_ACTIVE");

            _items.Clear();
        }

        public void MarkAsCheckedOut()
        {
            if (Status != CartStatus.Active)
                throw new BusinessException("CART_NOT_ACTIVE");

            if (_items.Count == 0)
                throw new BusinessException("CART_IS_EMPTY");

            Status = CartStatus.CheckedOut;
        }

        public void MarkAsAbandoned()
        {
            if (Status == CartStatus.CheckedOut)
                throw new BusinessException("CANNOT_ABANDON_CHECKED_OUT_CART");

            Status = CartStatus.Abandoned;
        }

        public void AddItem(CartItem item)
        {
            if (Status != CartStatus.Active)
                throw new BusinessException("CART_NOT_ACTIVE");

            _items.Add(item);
        }

        public void SetExtensionTimeout(DateTime? timeoutAt)
        {
            ExtensionTimeoutAt = timeoutAt;
        }

        // Query Methods

        public bool IsActive()
        {
            return Status == CartStatus.Active;
        }

        public bool IsEmpty()
        {
            return _items.Count == 0;
        }

        public int GetItemCount()
        {
            return _items.Count;
        }

        public decimal GetTotalAmount()
        {
            return _items.Sum(item => item.GetTotalPrice());
        }

        public int GetTotalDays()
        {
            return _items.Sum(item => item.GetDaysCount());
        }

        public bool HasBoothInCart(Guid boothId)
        {
            return _items.Any(item => item.BoothId == boothId);
        }

        public bool HasOverlappingBooking(Guid boothId, DateTime startDate, DateTime endDate)
        {
            return _items.Any(item =>
                item.BoothId == boothId &&
                item.OverlapsWith(startDate, endDate));
        }
    }
}