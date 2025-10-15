using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MP.Account
{
    public interface IUserProfileAppService : IApplicationService
    {
        Task<UserProfileDto> GetAsync();
        Task<UserProfileDto> UpdateAsync(UserProfileDto input);
    }
}