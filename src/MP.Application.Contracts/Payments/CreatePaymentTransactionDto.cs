using System;
using System.ComponentModel.DataAnnotations;

namespace MP.Payments
{
    public class CreatePaymentTransactionDto
    {
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

        [StringLength(1000)]
        public string? ReturnUrl { get; set; }

        [StringLength(1000)]
        public string? Statement { get; set; }

        [StringLength(2000)]
        public string? ExtraProperties { get; set; }

        public Guid? RentalId { get; set; }
    }
}