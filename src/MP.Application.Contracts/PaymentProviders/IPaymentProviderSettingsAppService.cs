using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MP.Application.Contracts.PaymentProviders
{
    public interface IPaymentProviderSettingsAppService : IApplicationService
    {
        Task<PaymentProviderSettingsDto> GetAsync();
        Task UpdateAsync(UpdatePaymentProviderSettingsDto input);
    }
}