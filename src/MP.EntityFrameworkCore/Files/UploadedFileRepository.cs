using System;
using MP.Domain.Files;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace MP.EntityFrameworkCore.Files
{
    public class UploadedFileRepository : EfCoreRepository<MPDbContext, UploadedFile, Guid>, IUploadedFileRepository
    {
        public UploadedFileRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }
    }
}
