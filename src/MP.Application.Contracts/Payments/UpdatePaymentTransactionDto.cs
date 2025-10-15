using System;
using System.ComponentModel.DataAnnotations;

namespace MP.Payments
{
    public class UpdatePaymentTransactionDto
    {
        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        public DateTime? LastStatusCheck { get; set; }

        [StringLength(10)]
        public string? Method { get; set; }

        [StringLength(255)]
        public string? TransferLabel { get; set; }

        [StringLength(1000)]
        public string? ReturnUrl { get; set; }

        [StringLength(1000)]
        public string? Statement { get; set; }

        [StringLength(2000)]
        public string? ExtraProperties { get; set; }

        public bool? Verified { get; set; }

        public int? ManualStatusCheckCount { get; set; }
    }
}