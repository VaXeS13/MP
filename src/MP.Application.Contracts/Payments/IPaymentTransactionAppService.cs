using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using MP.Payments;

namespace MP.Payments
{
    public interface IPaymentTransactionAppService : ICrudAppService<
        PaymentTransactionDto,
        Guid,
        GetPaymentTransactionListDto,
        CreatePaymentTransactionDto,
        UpdatePaymentTransactionDto>
    {
        Task<PaymentSuccessViewModel> GetPaymentSuccessViewModelAsync(string sessionId);
    }
}