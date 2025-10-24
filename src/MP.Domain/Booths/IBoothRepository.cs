using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.Booths
{
    public interface IBoothRepository : IRepository<Booth, Guid>
    {
        Task<Booth?> FindByNumberAsync(string number, CancellationToken cancellationToken = default);

        Task<List<Booth>> GetAvailableBoothsAsync(CancellationToken cancellationToken = default);

        Task<List<Booth>> GetAvailableBoothsAsync(Guid organizationalUnitId, CancellationToken cancellationToken = default);

        Task<bool> IsNumberUniqueAsync(string number, Guid? excludeId = null, CancellationToken cancellationToken = default);

        Task<List<Booth>> GetListWithActiveRentalsAsync(
            int skipCount,
            int maxResultCount,
            string? filter = null,
            BoothStatus? status = null,
            CancellationToken cancellationToken = default);

        Task<List<Booth>> GetListWithActiveRentalsAsync(
            int skipCount,
            int maxResultCount,
            Guid organizationalUnitId,
            string? filter = null,
            BoothStatus? status = null,
            CancellationToken cancellationToken = default);

        Task<int> GetCountAsync(string? filter = null, BoothStatus? status = null, CancellationToken cancellationToken = default);

        Task<int> GetCountAsync(Guid organizationalUnitId, string? filter = null, BoothStatus? status = null, CancellationToken cancellationToken = default);
    }

}
