using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MP.Tenants
{
    public interface ITenantCurrencyAppService : IApplicationService
    {
        Task<TenantCurrencyDto> GetTenantCurrencyAsync();
        Task UpdateTenantCurrencyAsync(TenantCurrencyDto input);
    }
}
