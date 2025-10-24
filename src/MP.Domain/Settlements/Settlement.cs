using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using MP.Domain.Items;

namespace MP.Domain.Settlements
{
    /// <summary>
    /// Customer settlement/payout entity
    /// </summary>
    public class Settlement : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; private set; }
        public Guid OrganizationalUnitId { get; private set; }
        public Guid UserId { get; private set; }
        public string SettlementNumber { get; private set; } = null!;
        public SettlementStatus Status { get; private set; }
        public decimal TotalAmount { get; private set; }
        public decimal CommissionAmount { get; private set; }
        public decimal NetAmount { get; private set; }
        public string? Notes { get; private set; }
        public string? BankAccountNumber { get; private set; }
        public DateTime? ProcessedAt { get; private set; }
        public DateTime? PaidAt { get; private set; }
        public Guid? ProcessedBy { get; private set; }
        public string? TransactionReference { get; private set; }
        public string? RejectionReason { get; private set; }
        public PaymentMethod? PaymentMethod { get; private set; }
        public string? PaymentProviderMetadata { get; private set; }

        private readonly List<SettlementItem> _items = new();
        public IReadOnlyList<SettlementItem> Items => _items.AsReadOnly();

        private Settlement() { }

        public Settlement(
            Guid id,
            Guid userId,
            Guid organizationalUnitId,
            string settlementNumber,
            string? notes = null,
            string? bankAccountNumber = null,
            Guid? tenantId = null) : base(id)
        {
            TenantId = tenantId;
            OrganizationalUnitId = organizationalUnitId;
            UserId = userId;
            SetSettlementNumber(settlementNumber);
            Notes = notes?.Trim();
            BankAccountNumber = bankAccountNumber?.Trim();
            Status = SettlementStatus.Pending;
            TotalAmount = 0;
            CommissionAmount = 0;
            NetAmount = 0;
        }

        public void SetSettlementNumber(string settlementNumber)
        {
            if (string.IsNullOrWhiteSpace(settlementNumber))
                throw new BusinessException("SETTLEMENT_NUMBER_REQUIRED");

            SettlementNumber = settlementNumber.Trim();
        }

        public void AddItem(Guid itemSheetItemId, decimal salePrice, decimal commissionPercentage)
        {
            if (Status != SettlementStatus.Pending)
                throw new BusinessException("CANNOT_ADD_ITEMS_TO_NON_PENDING_SETTLEMENT");

            if (_items.Any(i => i.ItemSheetItemId == itemSheetItemId))
                throw new BusinessException("ITEM_ALREADY_IN_SETTLEMENT");

            var commissionAmount = salePrice * (commissionPercentage / 100);
            var customerAmount = salePrice - commissionAmount;

            var item = new SettlementItem(
                Guid.NewGuid(),
                Id,
                itemSheetItemId,
                salePrice,
                commissionAmount,
                customerAmount);

            _items.Add(item);
            RecalculateTotals();
        }

        public void RemoveItem(Guid itemSheetItemId)
        {
            if (Status != SettlementStatus.Pending)
                throw new BusinessException("CANNOT_REMOVE_ITEMS_FROM_NON_PENDING_SETTLEMENT");

            var item = _items.FirstOrDefault(i => i.ItemSheetItemId == itemSheetItemId);
            if (item == null)
                throw new BusinessException("ITEM_NOT_FOUND_IN_SETTLEMENT");

            _items.Remove(item);
            RecalculateTotals();
        }

        public void Process(Guid processedBy)
        {
            if (Status != SettlementStatus.Pending)
                throw new BusinessException("CAN_ONLY_PROCESS_PENDING_SETTLEMENTS");

            if (_items.Count == 0)
                throw new BusinessException("CANNOT_PROCESS_EMPTY_SETTLEMENT");

            Status = SettlementStatus.Processing;
            ProcessedBy = processedBy;
            ProcessedAt = DateTime.Now;
        }

        public void Complete(string? transactionReference = null)
        {
            if (Status != SettlementStatus.Processing)
                throw new BusinessException("CAN_ONLY_COMPLETE_PROCESSING_SETTLEMENTS");

            Status = SettlementStatus.Completed;
            PaidAt = DateTime.Now;
            TransactionReference = transactionReference?.Trim();
        }

        public void Reject(string reason)
        {
            if (Status == SettlementStatus.Completed)
                throw new BusinessException("CANNOT_REJECT_COMPLETED_SETTLEMENT");

            Status = SettlementStatus.Cancelled;
            RejectionReason = reason?.Trim();
        }

        public void Cancel()
        {
            if (Status == SettlementStatus.Completed)
                throw new BusinessException("CANNOT_CANCEL_COMPLETED_SETTLEMENT");

            Status = SettlementStatus.Cancelled;
        }

        public void SetNotes(string? notes)
        {
            Notes = notes?.Trim();
        }

        public void SetBankAccountNumber(string? bankAccountNumber)
        {
            if (Status != SettlementStatus.Pending)
                throw new BusinessException("CANNOT_CHANGE_BANK_ACCOUNT_FOR_NON_PENDING_SETTLEMENT");

            BankAccountNumber = bankAccountNumber?.Trim();
        }

        private void RecalculateTotals()
        {
            TotalAmount = _items.Sum(i => i.SalePrice);
            CommissionAmount = _items.Sum(i => i.CommissionAmount);
            NetAmount = _items.Sum(i => i.CustomerAmount);
        }

        public int GetItemsCount() => _items.Count;

        public bool CanEdit() => Status == SettlementStatus.Pending;

        public bool CanProcess() => Status == SettlementStatus.Pending && _items.Count > 0;

        public bool CanComplete() => Status == SettlementStatus.Processing;

        public void SetPaymentMethod(PaymentMethod? paymentMethod, string? providerMetadata = null)
        {
            if (Status == SettlementStatus.Completed)
                throw new BusinessException("CANNOT_CHANGE_PAYMENT_METHOD_FOR_COMPLETED_SETTLEMENT");

            PaymentMethod = paymentMethod;
            PaymentProviderMetadata = providerMetadata?.Trim();
        }
    }

    /// <summary>
    /// Settlement item (sold item from item sheet)
    /// </summary>
    public class SettlementItem : CreationAuditedEntity<Guid>
    {
        public Guid SettlementId { get; private set; }
        public Guid ItemSheetItemId { get; private set; }
        public decimal SalePrice { get; private set; }
        public decimal CommissionAmount { get; private set; }
        public decimal CustomerAmount { get; private set; }

        // Navigation property
        public ItemSheetItem? ItemSheetItem { get; set; }

        private SettlementItem() { }

        public SettlementItem(
            Guid id,
            Guid settlementId,
            Guid itemSheetItemId,
            decimal salePrice,
            decimal commissionAmount,
            decimal customerAmount) : base(id)
        {
            SettlementId = settlementId;
            ItemSheetItemId = itemSheetItemId;
            SalePrice = salePrice;
            CommissionAmount = commissionAmount;
            CustomerAmount = customerAmount;
        }
    }

    /// <summary>
    /// Settlement status
    /// </summary>
    public enum SettlementStatus
    {
        Pending = 0,
        Processing = 1,
        Completed = 2,
        Cancelled = 3
    }

    /// <summary>
    /// Payment method for settlement payout
    /// </summary>
    public enum PaymentMethod
    {
        Manual = 0,
        BankTransfer = 1,
        StripePayouts = 2
    }
}
