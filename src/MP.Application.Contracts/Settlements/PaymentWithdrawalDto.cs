using System;
using Volo.Abp.Application.Dtos;

namespace MP.Application.Contracts.Settlements
{
    /// <summary>
    /// DTO for payment withdrawal request (admin view of settlement)
    /// </summary>
    public class PaymentWithdrawalDto : EntityDto<Guid>
    {
        public string SettlementNumber { get; set; } = null!;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string UserEmail { get; set; } = null!;
        public string? BankAccountNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal NetAmount { get; set; }
        public string Status { get; set; } = null!;
        public int ItemsCount { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? ProcessedByUserName { get; set; }
        public string? TransactionReference { get; set; }
        public string? RejectionReason { get; set; }
        public string? Notes { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentProviderMetadata { get; set; }
    }

    /// <summary>
    /// Request DTO for processing payment withdrawal
    /// </summary>
    public class ProcessWithdrawalDto
    {
        public Guid SettlementId { get; set; }
        public string PaymentMethod { get; set; } = null!; // Manual, BankTransfer, StripePayouts
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request DTO for completing payment withdrawal
    /// </summary>
    public class CompleteWithdrawalDto
    {
        public Guid SettlementId { get; set; }
        public string? TransactionReference { get; set; }
        public string? ProviderMetadata { get; set; }
    }

    /// <summary>
    /// Request DTO for rejecting payment withdrawal
    /// </summary>
    public class RejectWithdrawalDto
    {
        public Guid SettlementId { get; set; }
        public string Reason { get; set; } = null!;
    }

    /// <summary>
    /// Statistics for payment withdrawals dashboard
    /// </summary>
    public class PaymentWithdrawalStatsDto
    {
        public int PendingCount { get; set; }
        public decimal PendingAmount { get; set; }
        public int ProcessingCount { get; set; }
        public decimal ProcessingAmount { get; set; }
        public int CompletedThisMonthCount { get; set; }
        public decimal CompletedThisMonthAmount { get; set; }
    }
}
