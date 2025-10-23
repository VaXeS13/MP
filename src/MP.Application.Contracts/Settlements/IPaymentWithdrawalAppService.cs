using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MP.Application.Contracts.Settlements
{
    /// <summary>
    /// Application service for managing payment withdrawals (admin operations on settlements)
    /// </summary>
    public interface IPaymentWithdrawalAppService : IApplicationService
    {
        /// <summary>
        /// Get list of payment withdrawal requests with pagination and filtering
        /// </summary>
        Task<PagedResultDto<PaymentWithdrawalDto>> GetListAsync(PagedAndSortedResultRequestDto input);

        /// <summary>
        /// Get statistics for payment withdrawals dashboard
        /// </summary>
        Task<PaymentWithdrawalStatsDto> GetStatsAsync();

        /// <summary>
        /// Get single payment withdrawal request details
        /// </summary>
        Task<PaymentWithdrawalDto> GetAsync(Guid id);

        /// <summary>
        /// Mark withdrawal as processing and assign to current admin
        /// </summary>
        Task<PaymentWithdrawalDto> ProcessAsync(ProcessWithdrawalDto input);

        /// <summary>
        /// Complete withdrawal and mark as paid
        /// </summary>
        Task<PaymentWithdrawalDto> CompleteAsync(CompleteWithdrawalDto input);

        /// <summary>
        /// Reject withdrawal request with reason
        /// </summary>
        Task<PaymentWithdrawalDto> RejectAsync(RejectWithdrawalDto input);

        /// <summary>
        /// Execute payout via Stripe (if payment method is StripePayouts)
        /// </summary>
        Task<PaymentWithdrawalDto> ExecuteStripePayoutAsync(Guid settlementId);
    }
}
