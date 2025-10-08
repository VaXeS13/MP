using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MP.Application.Contracts.Payments
{
    public interface IPaymentProviderAppService : IApplicationService
    {
        Task<List<PaymentProviderDto>> GetAvailableProvidersAsync();
        Task<List<PaymentMethodDto>> GetPaymentMethodsAsync(string providerId, string currency);
        Task<PaymentCreationResultDto> CreatePaymentAsync(CreatePaymentRequestDto request);
    }
}