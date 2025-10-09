using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;
using Volo.Abp;
using MP.Domain.Rentals.Events;
using Volo.Abp.Identity;
using AutoFixture;
using MP.Domain.Booths;
using MP.Rentals;
using MP.Domain.Items;

namespace MP.Domain.Rentals
{
    public class Rental : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; private set; }
        public Guid UserId { get; private set; }
        public Guid BoothId { get; private set; }
        public Guid BoothTypeId { get; private set; }

        public RentalPeriod Period { get; private set; } = null!;
        public Payment Payment { get; private set; } = null!;
        public RentalStatus Status { get; private set; }

        // Szczegóły wynajęcia
        // Szczegóły wynajęcia
        public string? Notes { get; private set; }
        public DateTime? StartedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }

        // Navigation properties (dla wygody w queries)
        public IdentityUser User { get; set; } = null!;
        public Booth Booth { get; set; } = null!;
        public BoothTypes.BoothType BoothType { get; set; } = null!;

        private readonly List<ItemSheet> _itemSheets = new();
        public IReadOnlyList<ItemSheet> ItemSheets => _itemSheets.AsReadOnly();

        private Rental() { }

        public Rental(
            Guid id,
            Guid userId,
            Guid boothId,
            Guid boothTypeId,
            RentalPeriod period,
            decimal totalCost,
            Guid? tenantId = null) : base(id)
        {
            TenantId = tenantId;
            UserId = userId;
            BoothId = boothId;
            BoothTypeId = boothTypeId;
            Period = period;
            Payment = new Payment(totalCost);
            Status = RentalStatus.Draft;
        }

        // Domain Methods

        public void ConfirmRental(DateTime paidDate)
        {
            if (Status != RentalStatus.Draft)
                throw new BusinessException("RENTAL_ALREADY_CONFIRMED");

            if (!Payment.IsPaid)
                throw new BusinessException("RENTAL_MUST_BE_PAID_BEFORE_CONFIRM");

            Status = RentalStatus.Active;

            AddLocalEvent(new RentalConfirmedEvent(this));
        }

        public void StartRental()
        {
            if (Status != RentalStatus.Active)
                throw new BusinessException("RENTAL_NOT_ACTIVE");

            if (StartedAt.HasValue)
                throw new BusinessException("RENTAL_ALREADY_STARTED");

            if (DateTime.Today < Period.StartDate)
                throw new BusinessException("RENTAL_START_DATE_NOT_REACHED");

            StartedAt = DateTime.Now;
        }

        public void CompleteRental()
        {
            if (Status != RentalStatus.Active)
                throw new BusinessException("RENTAL_NOT_ACTIVE");

            if (!StartedAt.HasValue)
                throw new BusinessException("RENTAL_NOT_STARTED");

            if (CompletedAt.HasValue)
                throw new BusinessException("RENTAL_ALREADY_COMPLETED");

            CompletedAt = DateTime.Now;
            Status = RentalStatus.Expired;

            AddLocalEvent(new RentalCompletedEvent(this));
        }

        public void Cancel(string reason)
        {
            if (Status == RentalStatus.Cancelled)
                throw new BusinessException("RENTAL_ALREADY_CANCELLED");

            if (Status == RentalStatus.Expired)
                throw new BusinessException("CANNOT_CANCEL_EXPIRED_RENTAL");

            Status = RentalStatus.Cancelled;
            Notes = reason;

            AddLocalEvent(new RentalCancelledEvent(this));
        }

        public void AutoExpire()
        {
            if (Status != RentalStatus.Active && Status != RentalStatus.Extended)
                throw new BusinessException("CAN_ONLY_EXPIRE_ACTIVE_OR_EXTENDED_RENTAL");

            if (DateTime.Today <= Period.EndDate)
                throw new BusinessException("RENTAL_PERIOD_NOT_ENDED_YET");

            CompletedAt = DateTime.Now;
            Status = RentalStatus.Expired;

            AddLocalEvent(new RentalCompletedEvent(this));
        }

        public void MarkAsPaid(decimal amount, DateTime paidDate, string? transactionId = null)
        {
            Payment.MarkAsPaid(amount, paidDate, transactionId);

            if (Payment.IsPaid && Status == RentalStatus.Draft)
            {
                ConfirmRental(paidDate);
            }
        }


        public void ExtendRental(RentalPeriod newPeriod, decimal additionalCost)
        {
            if (Status != RentalStatus.Active)
                throw new BusinessException("CAN_ONLY_EXTEND_ACTIVE_RENTAL");

            if (newPeriod.StartDate != Period.StartDate)
                throw new BusinessException("EXTENSION_MUST_KEEP_SAME_START_DATE");

            if (newPeriod.EndDate <= Period.EndDate)
                throw new BusinessException("EXTENSION_MUST_INCREASE_END_DATE");

            Period = newPeriod;

            // Dodaj dodatkowy koszt do płatności
            var newTotalCost = Payment.TotalAmount + additionalCost;
            Payment = new Payment(newTotalCost);

            Status = RentalStatus.Extended;

            AddLocalEvent(new RentalExtendedEvent(this, additionalCost));
        }

        // Query methods
        public bool IsActive()
        {
            return Status == RentalStatus.Active || Status == RentalStatus.Extended;
        }

        public bool IsExpired()
        {
            return Status == RentalStatus.Expired ||
                   IsActive() && DateTime.Today > Period.EndDate;
        }

        public bool IsOverdue()
        {
            return IsActive() && DateTime.Today > Period.EndDate.AddDays(7); // 7 dni na odebranie
        }

        public decimal GetTotalCommissionEarned()
        {
            return _itemSheets
                .SelectMany(sheet => sheet.Items)
                .Where(item => item.Status == ItemSheetItemStatus.Sold && item.Item != null)
                .Sum(item => item.GetCommissionAmount(item.Item.Price));
        }

        public int GetItemsCount()
        {
            return _itemSheets.Sum(sheet => sheet.GetItemsCount());
        }

        public int GetSoldItemsCount()
        {
            return _itemSheets.Sum(sheet => sheet.GetSoldItemsCount());
        }

        public decimal GetTotalSalesAmount()
        {
            return _itemSheets
                .SelectMany(sheet => sheet.Items)
                .Where(item => item.Status == ItemSheetItemStatus.Sold && item.Item != null)
                .Sum(item => item.Item.Price);
        }

        public void SetNotes(string? notes)
        {
            Notes = notes?.Trim();
        }
    }
}