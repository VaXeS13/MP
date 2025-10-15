using System;
using Volo.Abp.Application.Dtos;

namespace MP.Payments
{
    public class PaymentTransactionDto : FullAuditedEntityDto<Guid>
    {
        public string SessionId { get; set; } = null!;
        public int MerchantId { get; set; }
        public int PosId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? Method { get; set; }
        public string? TransferLabel { get; set; }
        public string Sign { get; set; } = null!;
        public string? OrderId { get; set; }
        public bool Verified { get; set; }
        public string? ReturnUrl { get; set; }
        public string? Statement { get; set; }
        public string? ExtraProperties { get; set; }
        public int ManualStatusCheckCount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? LastStatusCheck { get; set; }
        public Guid? RentalId { get; set; }

        // Dodatkowe wygodne właściwości
        public string StatusDisplayName { get; set; } = null!;
        public bool IsCompleted { get; set; }
        public bool IsFailed { get; set; }
        public bool IsPending { get; set; }
        public bool IsVerified { get; set; }
        public string FormattedAmount { get; set; } = null!;
        public string FormattedCreatedAt { get; set; } = null!;
        public string? FormattedCompletedAt { get; set; }
        public string PaymentMethodDisplayName { get; set; } = "Przelewy24";

        // Dla celów strony sukcesu - mapujemy SessionId jako TransactionGuid
        public string TransactionGuid => SessionId;
        public DateTime CreatedAt => CreationTime;
        public DateTime? CompletedAt => LastStatusCheck;
    }
}