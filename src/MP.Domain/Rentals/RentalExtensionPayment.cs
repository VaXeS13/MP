using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;
using MP.Rentals;

namespace MP.Domain.Rentals
{
    public class RentalExtensionPayment : Entity<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; private set; }
        public Guid RentalId { get; private set; }
        public DateTime OldEndDate { get; private set; }
        public DateTime NewEndDate { get; private set; }
        public decimal ExtensionCost { get; private set; }
        public ExtensionPaymentType PaymentType { get; private set; }
        public DateTime ExtendedAt { get; private set; }
        public Guid ExtendedBy { get; private set; }
        public string? TransactionId { get; private set; }
        public string? ReceiptNumber { get; private set; }

        private RentalExtensionPayment() { }

        public RentalExtensionPayment(
            Guid id,
            Guid rentalId,
            DateTime oldEndDate,
            DateTime newEndDate,
            decimal extensionCost,
            ExtensionPaymentType paymentType,
            Guid extendedBy,
            string? transactionId = null,
            string? receiptNumber = null,
            Guid? tenantId = null) : base(id)
        {
            RentalId = rentalId;
            OldEndDate = oldEndDate;
            NewEndDate = newEndDate;
            ExtensionCost = extensionCost;
            PaymentType = paymentType;
            ExtendedAt = DateTime.Now;
            ExtendedBy = extendedBy;
            TransactionId = transactionId;
            ReceiptNumber = receiptNumber;
            TenantId = tenantId;
        }
    }
}
