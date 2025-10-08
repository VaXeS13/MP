using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.Payments
{
    public class P24Transaction : FullAuditedEntity<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }

        [Required]
        [StringLength(255)]
        public string SessionId { get; set; } = null!;

        [Required]
        public int MerchantId { get; set; }

        [Required]
        public int PosId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "PLN";

        [Required]
        [StringLength(255)]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = null!;

        [StringLength(10)]
        public string? Method { get; set; }

        [StringLength(255)]
        public string? TransferLabel { get; set; }

        [Required]
        [StringLength(512)]
        public string Sign { get; set; } = null!;

        [StringLength(255)]
        public string? OrderId { get; set; }

        public bool Verified { get; set; } = false;

        [StringLength(1000)]
        public string? ReturnUrl { get; set; }

        [StringLength(1000)]
        public string? Statement { get; set; }

        [StringLength(2000)]
        public string? ExtraProperties { get; set; }

        public int ManualStatusCheckCount { get; set; } = 0;

        // Status płatności z P24
        [StringLength(50)]
        public string Status { get; set; } = "processing";

        // Data ostatniej weryfikacji statusu
        public DateTime? LastStatusCheck { get; set; }

        // ID powiązanego wynajęcia (DEPRECATED - używaj SessionId do łączenia z Rental.Payment.Przelewy24TransactionId)
        // Dla transakcji cart checkout (wiele rentali) to pole jest opcjonalne
        // Pełna lista rental IDs jest przechowywana w ExtraProperties.RentalIds
        public Guid? RentalId { get; set; }

        protected P24Transaction()
        {
        }

        public P24Transaction(
            Guid id,
            string sessionId,
            int merchantId,
            int posId,
            decimal amount,
            string currency,
            string email,
            string description,
            string sign,
            Guid? tenantId = null)
            : base(id)
        {
            SessionId = sessionId;
            MerchantId = merchantId;
            PosId = posId;
            Amount = amount;
            Currency = currency;
            Email = email;
            Description = description;
            Sign = sign;
            TenantId = tenantId;
        }

        public void SetVerified(bool verified)
        {
            Verified = verified;
        }

        public void SetStatus(string status)
        {
            Status = status;
            LastStatusCheck = DateTime.UtcNow;
        }

        public void IncrementStatusCheckCount()
        {
            ManualStatusCheckCount++;
            LastStatusCheck = DateTime.UtcNow;
        }

        public void SetRentalId(Guid rentalId)
        {
            RentalId = rentalId;
        }
    }
}