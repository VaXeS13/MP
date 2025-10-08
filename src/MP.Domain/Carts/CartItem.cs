using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace MP.Domain.Carts
{
    public class CartItem : FullAuditedEntity<Guid>
    {
        public Guid CartId { get; private set; }
        public Guid BoothId { get; private set; }
        public Guid BoothTypeId { get; private set; }

        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }

        public decimal PricePerDay { get; private set; }
        public string? Notes { get; private set; }

        // Navigation property
        public Cart Cart { get; set; } = null!;

        private CartItem() { }

        public CartItem(
            Guid id,
            Guid cartId,
            Guid boothId,
            Guid boothTypeId,
            DateTime startDate,
            DateTime endDate,
            decimal pricePerDay,
            string? notes = null) : base(id)
        {
            CartId = cartId;
            BoothId = boothId;
            BoothTypeId = boothTypeId;
            SetPeriod(startDate, endDate);
            SetPricePerDay(pricePerDay);
            Notes = notes?.Trim();
        }

        public void SetPeriod(DateTime startDate, DateTime endDate)
        {
            if (startDate >= endDate)
                throw new BusinessException("CART_ITEM_INVALID_PERIOD")
                    .WithData("StartDate", startDate)
                    .WithData("EndDate", endDate);

            if (startDate < DateTime.Today)
                throw new BusinessException("CART_ITEM_START_DATE_IN_PAST");

            StartDate = startDate.Date;
            EndDate = endDate.Date;
        }

        public void SetPricePerDay(decimal pricePerDay)
        {
            if (pricePerDay <= 0)
                throw new BusinessException("CART_ITEM_INVALID_PRICE")
                    .WithData("PricePerDay", pricePerDay);

            PricePerDay = pricePerDay;
        }

        public void UpdateBoothType(Guid boothTypeId)
        {
            BoothTypeId = boothTypeId;
        }

        public void SetNotes(string? notes)
        {
            Notes = notes?.Trim();
        }

        public int GetDaysCount()
        {
            return (EndDate - StartDate).Days + 1; // Include both start and end date
        }

        public decimal GetTotalPrice()
        {
            return GetDaysCount() * PricePerDay;
        }

        public bool OverlapsWith(DateTime startDate, DateTime endDate)
        {
            return StartDate <= endDate && EndDate >= startDate;
        }
    }
}