using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.Payments
{
    /// <summary>
    /// PayPal transaction entity for tracking PayPal payments
    /// </summary>
    public class PayPalTransaction : FullAuditedEntity<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }

        /// <summary>
        /// PayPal Order ID (e.g., "5O190127TN364715T")
        /// </summary>
        [Required]
        [StringLength(255)]
        public string OrderId { get; set; } = null!;

        /// <summary>
        /// PayPal Payer ID (customer identifier)
        /// </summary>
        [StringLength(255)]
        public string? PayerId { get; set; }

        /// <summary>
        /// PayPal Payment ID (for older API versions)
        /// </summary>
        [StringLength(255)]
        public string? PaymentId { get; set; }

        /// <summary>
        /// PayPal Capture ID (after payment capture)
        /// </summary>
        [StringLength(255)]
        public string? CaptureId { get; set; }

        /// <summary>
        /// PayPal Authorization ID (for authorized but not captured payments)
        /// </summary>
        [StringLength(255)]
        public string? AuthorizationId { get; set; }

        /// <summary>
        /// PayPal Refund ID (if payment was refunded)
        /// </summary>
        [StringLength(255)]
        public string? RefundId { get; set; }

        /// <summary>
        /// Payment amount
        /// </summary>
        [Required]
        public decimal Amount { get; set; }

        /// <summary>
        /// Currency code (e.g., "PLN", "EUR", "USD")
        /// </summary>
        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "PLN";

        /// <summary>
        /// Payment description
        /// </summary>
        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = null!;

        /// <summary>
        /// Customer email from PayPal
        /// </summary>
        [Required]
        [StringLength(255)]
        public string Email { get; set; } = null!;

        /// <summary>
        /// PayPal environment (sandbox or live)
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Environment { get; set; } = "sandbox";

        /// <summary>
        /// PayPal order status (CREATED, APPROVED, VOIDED, COMPLETED, PAYER_ACTION_REQUIRED)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "CREATED";

        /// <summary>
        /// PayPal funding source (paypal, card, credit, etc.)
        /// </summary>
        [StringLength(50)]
        public string? FundingSource { get; set; }

        /// <summary>
        /// PayPal approval URL for customer redirect
        /// </summary>
        [StringLength(1000)]
        public string? ApprovalUrl { get; set; }

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
        /// PayPal Client ID used for this transaction
        /// </summary>
        [StringLength(255)]
        public string? ClientId { get; set; }

        /// <summary>
        /// Additional PayPal-specific data in JSON format
        /// </summary>
        [StringLength(4000)]
        public string? PayPalMetadata { get; set; }

        /// <summary>
        /// Webhook verification ID
        /// </summary>
        [StringLength(255)]
        public string? WebhookId { get; set; }

        /// <summary>
        /// Fee charged by PayPal
        /// </summary>
        public decimal? PayPalFee { get; set; }

        /// <summary>
        /// Net amount received after fees
        /// </summary>
        public decimal? NetAmount { get; set; }

        /// <summary>
        /// Date when payment was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Date when payment was approved by customer
        /// </summary>
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// Date when payment was captured
        /// </summary>
        public DateTime? CapturedAt { get; set; }

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
        /// Customer's PayPal account details (if shared)
        /// </summary>
        [StringLength(500)]
        public string? CustomerDetails { get; set; }

        /// <summary>
        /// Dispute case ID (if payment is disputed)
        /// </summary>
        [StringLength(255)]
        public string? DisputeId { get; set; }

        protected PayPalTransaction()
        {
        }

        public PayPalTransaction(
            Guid id,
            string orderId,
            decimal amount,
            string currency,
            string description,
            string email,
            string environment,
            Guid? tenantId = null)
            : base(id)
        {
            OrderId = orderId;
            Amount = amount;
            Currency = currency;
            Description = description;
            Email = email;
            Environment = environment;
            TenantId = tenantId;
        }

        public void SetStatus(string status)
        {
            Status = status;
            LastStatusCheck = DateTime.UtcNow;

            if (status == "COMPLETED")
            {
                CompletedAt = DateTime.UtcNow;
            }
            else if (status == "APPROVED")
            {
                ApprovedAt = DateTime.UtcNow;
            }
        }

        public void SetPayer(string payerId, string? customerDetails = null)
        {
            PayerId = payerId;
            CustomerDetails = customerDetails;
        }

        public void SetCapture(string captureId, decimal? paypalFee = null, decimal? netAmount = null)
        {
            CaptureId = captureId;
            PayPalFee = paypalFee;
            NetAmount = netAmount;
            CapturedAt = DateTime.UtcNow;
        }

        public void SetAuthorization(string authorizationId)
        {
            AuthorizationId = authorizationId;
        }

        public void SetRefund(string refundId, decimal refundAmount)
        {
            RefundId = refundId;
            // You might want to track refund amount separately
        }

        public void SetDispute(string disputeId)
        {
            DisputeId = disputeId;
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
            return Status == "COMPLETED";
        }

        public bool IsApproved()
        {
            return Status == "APPROVED";
        }

        public bool IsCancelled()
        {
            return Status == "VOIDED" || Status == "CANCELLED";
        }

        public bool RequiresPayerAction()
        {
            return Status == "PAYER_ACTION_REQUIRED";
        }
    }
}