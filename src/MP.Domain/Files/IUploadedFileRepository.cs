using System;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.Files
{
    public interface IUploadedFileRepository : IRepository<UploadedFile, Guid>
    {
    }
}
