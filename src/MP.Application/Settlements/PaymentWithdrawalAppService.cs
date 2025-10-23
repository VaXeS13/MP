using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;
using Volo.Abp.Identity;
using MP.Application.Contracts.Settlements;
using MP.Application.Payments;
using MP.Domain.Settlements;
using MP.Domain.Identity;

namespace MP.Application.Settlements
{
    /// <summary>
    /// Application service for managing payment withdrawals (admin operations)
    /// </summary>
    public class PaymentWithdrawalAppService : ApplicationService, IPaymentWithdrawalAppService
    {
        private readonly IRepository<Settlement, Guid> _settlementRepository;
        private readonly IRepository<IdentityUser, Guid> _userRepository;
        private readonly IRepository<UserProfile, Guid> _userProfileRepository;
        private readonly StripePayoutsService _stripePayoutsService;
        private readonly ILogger<PaymentWithdrawalAppService> _logger;

        public PaymentWithdrawalAppService(
            IRepository<Settlement, Guid> settlementRepository,
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<UserProfile, Guid> userProfileRepository,
            StripePayoutsService stripePayoutsService,
            ILogger<PaymentWithdrawalAppService> logger)
        {
            _settlementRepository = settlementRepository;
            _userRepository = userRepository;
            _userProfileRepository = userProfileRepository;
            _stripePayoutsService = stripePayoutsService;
            _logger = logger;
        }

        public async Task<PagedResultDto<PaymentWithdrawalDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            var queryable = await _settlementRepository.GetQueryableAsync();
            var userQueryable = await _userRepository.GetQueryableAsync();
            var profileQueryable = await _userProfileRepository.GetQueryableAsync();

            var query = from s in queryable
                        join u in userQueryable on s.UserId equals u.Id
                        join p in profileQueryable on s.UserId equals p.UserId into profiles
                        from profile in profiles.DefaultIfEmpty()
                        orderby s.CreationTime descending
                        select new PaymentWithdrawalDto
                        {
                            Id = s.Id,
                            SettlementNumber = s.SettlementNumber,
                            UserId = s.UserId,
                            UserName = u.UserName ?? "",
                            UserEmail = u.Email ?? "",
                            BankAccountNumber = profile != null ? profile.BankAccountNumber : null,
                            TotalAmount = s.TotalAmount,
                            CommissionAmount = s.CommissionAmount,
                            NetAmount = s.NetAmount,
                            Status = s.Status.ToString(),
                            ItemsCount = s.GetItemsCount(),
                            CreationTime = s.CreationTime,
                            ProcessedAt = s.ProcessedAt,
                            PaidAt = s.PaidAt,
                            TransactionReference = s.TransactionReference,
                            RejectionReason = s.RejectionReason,
                            Notes = s.Notes,
                            PaymentMethod = s.PaymentMethod != null ? s.PaymentMethod.ToString() : null,
                            PaymentProviderMetadata = s.PaymentProviderMetadata
                        };

            var totalCount = await AsyncExecuter.CountAsync(query);
            var items = await AsyncExecuter.ToListAsync(
                query.Skip(input.SkipCount).Take(input.MaxResultCount));

            return new PagedResultDto<PaymentWithdrawalDto>(totalCount, items);
        }

        public async Task<PaymentWithdrawalStatsDto> GetStatsAsync()
        {
            var queryable = await _settlementRepository.GetQueryableAsync();

            var stats = new PaymentWithdrawalStatsDto();

            // Pending
            var pending = await AsyncExecuter.ToListAsync(
                queryable.Where(s => s.Status == SettlementStatus.Pending));
            stats.PendingCount = pending.Count;
            stats.PendingAmount = pending.Sum(s => s.NetAmount);

            // Processing
            var processing = await AsyncExecuter.ToListAsync(
                queryable.Where(s => s.Status == SettlementStatus.Processing));
            stats.ProcessingCount = processing.Count;
            stats.ProcessingAmount = processing.Sum(s => s.NetAmount);

            // Completed this month
            var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var completedThisMonth = await AsyncExecuter.ToListAsync(
                queryable.Where(s => s.Status == SettlementStatus.Completed &&
                                     s.PaidAt >= firstDayOfMonth));
            stats.CompletedThisMonthCount = completedThisMonth.Count;
            stats.CompletedThisMonthAmount = completedThisMonth.Sum(s => s.NetAmount);

            return stats;
        }

        public async Task<PaymentWithdrawalDto> GetAsync(Guid id)
        {
            var settlement = await _settlementRepository.GetAsync(id);
            var user = await _userRepository.GetAsync(settlement.UserId);

            var profile = await _userProfileRepository.FirstOrDefaultAsync(p => p.UserId == settlement.UserId);

            return new PaymentWithdrawalDto
            {
                Id = settlement.Id,
                SettlementNumber = settlement.SettlementNumber,
                UserId = settlement.UserId,
                UserName = user.UserName ?? "",
                UserEmail = user.Email ?? "",
                BankAccountNumber = profile?.BankAccountNumber,
                TotalAmount = settlement.TotalAmount,
                CommissionAmount = settlement.CommissionAmount,
                NetAmount = settlement.NetAmount,
                Status = settlement.Status.ToString(),
                ItemsCount = settlement.GetItemsCount(),
                CreationTime = settlement.CreationTime,
                ProcessedAt = settlement.ProcessedAt,
                PaidAt = settlement.PaidAt,
                TransactionReference = settlement.TransactionReference,
                RejectionReason = settlement.RejectionReason,
                Notes = settlement.Notes,
                PaymentMethod = settlement.PaymentMethod?.ToString(),
                PaymentProviderMetadata = settlement.PaymentProviderMetadata
            };
        }

        public async Task<PaymentWithdrawalDto> ProcessAsync(ProcessWithdrawalDto input)
        {
            var settlement = await _settlementRepository.GetAsync(input.SettlementId);
            var currentUserId = CurrentUser.GetId();

            // Parse payment method
            if (!Enum.TryParse<Domain.Settlements.PaymentMethod>(input.PaymentMethod, out var paymentMethod))
            {
                throw new BusinessException("INVALID_PAYMENT_METHOD");
            }

            settlement.Process(currentUserId);
            settlement.SetPaymentMethod(paymentMethod);

            if (!string.IsNullOrEmpty(input.Notes))
            {
                settlement.SetNotes(input.Notes);
            }

            await _settlementRepository.UpdateAsync(settlement);

            _logger.LogInformation(
                "Settlement {SettlementId} marked as Processing by user {UserId}",
                settlement.Id, currentUserId);

            return await GetAsync(settlement.Id);
        }

        public async Task<PaymentWithdrawalDto> CompleteAsync(CompleteWithdrawalDto input)
        {
            var settlement = await _settlementRepository.GetAsync(input.SettlementId);

            settlement.Complete(input.TransactionReference);

            if (!string.IsNullOrEmpty(input.ProviderMetadata))
            {
                settlement.SetPaymentMethod(settlement.PaymentMethod, input.ProviderMetadata);
            }

            await _settlementRepository.UpdateAsync(settlement);

            _logger.LogInformation(
                "Settlement {SettlementId} completed with transaction reference {TransactionRef}",
                settlement.Id, input.TransactionReference);

            return await GetAsync(settlement.Id);
        }

        public async Task<PaymentWithdrawalDto> RejectAsync(RejectWithdrawalDto input)
        {
            var settlement = await _settlementRepository.GetAsync(input.SettlementId);

            settlement.Reject(input.Reason);

            await _settlementRepository.UpdateAsync(settlement);

            _logger.LogInformation(
                "Settlement {SettlementId} rejected with reason: {Reason}",
                settlement.Id, input.Reason);

            return await GetAsync(settlement.Id);
        }

        public async Task<PaymentWithdrawalDto> ExecuteStripePayoutAsync(Guid settlementId)
        {
            var settlement = await _settlementRepository.GetAsync(settlementId);
            var user = await _userRepository.GetAsync(settlement.UserId);

            if (settlement.Status != SettlementStatus.Processing)
            {
                throw new BusinessException("SETTLEMENT_MUST_BE_IN_PROCESSING_STATUS");
            }

            if (settlement.PaymentMethod != Domain.Settlements.PaymentMethod.StripePayouts)
            {
                throw new BusinessException("SETTLEMENT_PAYMENT_METHOD_MUST_BE_STRIPE_PAYOUTS");
            }

            var profile = await _userProfileRepository.FirstOrDefaultAsync(p => p.UserId == settlement.UserId);
            var bankAccountNumber = profile?.BankAccountNumber;
            if (string.IsNullOrEmpty(bankAccountNumber))
            {
                throw new BusinessException("USER_BANK_ACCOUNT_NUMBER_NOT_SET");
            }

            // Execute Stripe payout
            var result = await _stripePayoutsService.CreatePayoutAsync(
                settlementId: settlement.Id,
                amount: settlement.NetAmount,
                currency: "PLN", // TODO: Get from tenant settings
                bankAccountNumber: bankAccountNumber,
                description: $"Payout for settlement {settlement.SettlementNumber}");

            if (!result.Success)
            {
                throw new BusinessException("STRIPE_PAYOUT_FAILED")
                    .WithData("ErrorMessage", result.ErrorMessage);
            }

            // Update settlement with payout information
            var metadata = System.Text.Json.JsonSerializer.Serialize(result.Metadata);
            settlement.SetPaymentMethod(Domain.Settlements.PaymentMethod.StripePayouts, metadata);

            await _settlementRepository.UpdateAsync(settlement);

            _logger.LogInformation(
                "Stripe payout executed for settlement {SettlementId}, payout ID: {PayoutId}",
                settlement.Id, result.PayoutId);

            return await GetAsync(settlement.Id);
        }
    }
}
