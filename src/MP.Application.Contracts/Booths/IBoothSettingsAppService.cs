using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MP.Booths
{
    public interface IBoothSettingsAppService : IApplicationService
    {
        Task<BoothSettingsDto> GetAsync();
        Task UpdateAsync(BoothSettingsDto input);
    }
}
