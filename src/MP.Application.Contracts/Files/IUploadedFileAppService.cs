using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MP.Application.Contracts.Files
{
    public interface IUploadedFileAppService : IApplicationService
    {
        Task<UploadedFileDto> UploadAsync(UploadFileDto input);
        Task<UploadedFileDto> GetAsync(Guid id);
        Task DeleteAsync(Guid id);
    }
}
