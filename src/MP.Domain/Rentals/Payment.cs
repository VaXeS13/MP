using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Values;
using Volo.Abp;

namespace MP.Domain.Rentals
{
    public class Payment : ValueObject
    {
        public decimal TotalAmount { get; private set; }
        public decimal PaidAmount { get; private set; }
        public DateTime? PaidDate { get; private set; }
        public string? Przelewy24TransactionId { get; private set; }
        public PaymentStatus PaymentStatus { get; private set; }
        public bool IsPaid => PaidAmount >= TotalAmount && PaidDate.HasValue && PaymentStatus == PaymentStatus.Completed;

        private Payment() { } // Dla EF Core

        public Payment(decimal totalAmount)
        {
            if (totalAmount <= 0)
                throw new BusinessException("PAYMENT_AMOUNT_MUST_BE_POSITIVE");

            TotalAmount = totalAmount;
            PaidAmount = 0;
            PaymentStatus = PaymentStatus.Pending;
        }

        public void MarkAsPaid(decimal amount, DateTime paidDate, string? transactionId = null)
        {
            if (amount <= 0)
                throw new BusinessException("PAID_AMOUNT_MUST_BE_POSITIVE");

            if (paidDate > DateTime.Now)
                throw new BusinessException("PAID_DATE_CANNOT_BE_IN_FUTURE");

            PaidAmount = amount;
            PaidDate = paidDate;
            // Only update TransactionId if a new value is provided, otherwise keep existing value
            if (!string.IsNullOrEmpty(transactionId))
            {
                Przelewy24TransactionId = transactionId;
            }
            PaymentStatus = PaymentStatus.Completed;
        }

        public void SetTransactionId(string transactionId)
        {
            Przelewy24TransactionId = transactionId;
            PaymentStatus = PaymentStatus.Processing;
        }

        public void MarkAsFailed()
        {
            PaymentStatus = PaymentStatus.Failed;
        }

        public void MarkAsCancelled()
        {
            PaymentStatus = PaymentStatus.Cancelled;
        }

        public decimal GetRemainingAmount()
        {
            return Math.Max(0, TotalAmount - PaidAmount);
        }
        
        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return TotalAmount;
            yield return PaidAmount;
            yield return PaidDate;
            yield return Przelewy24TransactionId ?? string.Empty;
            yield return PaymentStatus;
        }
    }
}