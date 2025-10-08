using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.BoothTypes
{
    public interface IBoothTypeRepository : IRepository<BoothType, Guid>
    {
        Task<List<BoothType>> GetActiveTypesAsync(CancellationToken cancellationToken = default);
        Task<BoothType?> FindByNameAsync(string name, CancellationToken cancellationToken = default);
        Task<bool> IsNameUniqueAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    }
}