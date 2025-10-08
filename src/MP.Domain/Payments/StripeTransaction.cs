using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.Payments
{
    /// <summary>
    /// Stripe transaction entity for tracking Stripe payments
    /// </summary>
    public class StripeTransaction : FullAuditedEntity<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }

        /// <summary>
        /// Stripe PaymentIntent ID (e.g., "pi_1234567890")
        /// </summary>
        [Required]
        [StringLength(255)]
        public string PaymentIntentId { get; set; } = null!;

        /// <summary>
        /// PaymentIntent client secret for frontend processing
        /// </summary>
        [StringLength(512)]
        public string? ClientSecret { get; set; }

        /// <summary>
        /// Stripe Customer ID if customer exists
        /// </summary>
        [StringLength(255)]
        public string? CustomerId { get; set; }

        /// <summary>
        /// Payment method ID used for this transaction
        /// </summary>
        [StringLength(255)]
        public string? PaymentMethodId { get; set; }

        /// <summary>
        /// SetupIntent ID if saving payment method for future use
        /// </summary>
        [StringLength(255)]
        public string? SetupIntentId { get; set; }

        /// <summary>
        /// Amount in cents (Stripe uses smallest currency unit)
        /// </summary>
        [Required]
        public long AmountCents { get; set; }

        /// <summary>
        /// Amount in original currency (for display)
        /// </summary>
        [Required]
        public decimal Amount { get; set; }

        /// <summary>
        /// Currency code (e.g., "pln", "eur", "usd")
        /// </summary>
        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "pln";

        /// <summary>
        /// Payment description
        /// </summary>
        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = null!;

        /// <summary>
        /// Customer email
        /// </summary>
        [Required]
        [StringLength(255)]
        public string Email { get; set; } = null!;

        /// <summary>
        /// Stripe payment status (requires_payment_method, requires_confirmation, processing, succeeded, canceled)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "requires_payment_method";

        /// <summary>
        /// Payment method type (card, apple_pay, google_pay, klarna, etc.)
        /// </summary>
        [StringLength(50)]
        public string? PaymentMethodType { get; set; }

        /// <summary>
        /// Return URL after payment completion
        /// </summary>
        [StringLength(1000)]
        public string? ReturnUrl { get; set; }

        /// <summary>
        /// Cancel URL if payment is cancelled
        /// </summary>
        [StringLength(1000)]
        public string? CancelUrl { get; set; }

        /// <summary>
        /// Additional Stripe-specific data in JSON format
        /// </summary>
        [StringLength(4000)]
        public string? StripeMetadata { get; set; }

        /// <summary>
        /// Webhook endpoint secret for verification
        /// </summary>
        [StringLength(512)]
        public string? WebhookSecret { get; set; }

        /// <summary>
        /// Date when payment was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Date of last status check
        /// </summary>
        public DateTime? LastStatusCheck { get; set; }

        /// <summary>
        /// Number of manual status checks performed
        /// </summary>
        public int StatusCheckCount { get; set; } = 0;

        /// <summary>
        /// Associated rental ID
        /// </summary>
        public Guid? RentalId { get; set; }

        /// <summary>
        /// Stripe charge ID (after successful payment)
        /// </summary>
        [StringLength(255)]
        public string? ChargeId { get; set; }

        /// <summary>
        /// Fee charged by Stripe (in cents)
        /// </summary>
        public long? StripeFee { get; set; }

        /// <summary>
        /// Network transaction ID for bank transfers
        /// </summary>
        [StringLength(255)]
        public string? NetworkTransactionId { get; set; }

        protected StripeTransaction()
        {
        }

        public StripeTransaction(
            Guid id,
            string paymentIntentId,
            long amountCents,
            decimal amount,
            string currency,
            string description,
            string email,
            Guid? tenantId = null)
            : base(id)
        {
            PaymentIntentId = paymentIntentId;
            AmountCents = amountCents;
            Amount = amount;
            Currency = currency;
            Description = description;
            Email = email;
            TenantId = tenantId;
        }

        public void SetStatus(string status)
        {
            Status = status;
            LastStatusCheck = DateTime.UtcNow;

            if (status == "succeeded")
            {
                CompletedAt = DateTime.UtcNow;
            }
        }

        public void SetPaymentMethod(string paymentMethodId, string paymentMethodType)
        {
            PaymentMethodId = paymentMethodId;
            PaymentMethodType = paymentMethodType;
        }

        public void SetCustomer(string customerId)
        {
            CustomerId = customerId;
        }

        public void SetCharge(string chargeId, long? stripeFee = null, string? networkTransactionId = null)
        {
            ChargeId = chargeId;
            StripeFee = stripeFee;
            NetworkTransactionId = networkTransactionId;
        }

        public void SetRentalId(Guid rentalId)
        {
            RentalId = rentalId;
        }

        public void IncrementStatusCheckCount()
        {
            StatusCheckCount++;
            LastStatusCheck = DateTime.UtcNow;
        }

        public bool IsCompleted()
        {
            return Status == "succeeded";
        }

        public bool IsFailed()
        {
            return Status == "canceled" || Status == "requires_payment_method";
        }
    }
}