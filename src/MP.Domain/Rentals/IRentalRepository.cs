using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.Rentals
{
    public interface IRentalRepository : IRepository<Rental, Guid>
    {
        Task<List<Rental>> GetRentalsForUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<List<Rental>> GetActiveRentalsAsync(
            CancellationToken cancellationToken = default);

        Task<List<Rental>> GetExpiredRentalsAsync(
            DateTime beforeDate,
            CancellationToken cancellationToken = default);

        Task<Rental?> GetRentalWithItemsAsync(
            Guid rentalId,
            CancellationToken cancellationToken = default);

        Task<bool> HasActiveRentalForBoothAsync(
            Guid boothId,
            DateTime startDate,
            DateTime endDate,
            Guid? excludeRentalId = null,
            CancellationToken cancellationToken = default);

        Task<List<Rental>> GetRentalsForBoothAsync(
            Guid boothId,
            CancellationToken cancellationToken = default);

        Task<decimal> GetTotalRevenueAsync(
            DateTime fromDate,
            DateTime toDate,
            CancellationToken cancellationToken = default);

        Task<Rental?> GetNearestRentalBeforeAsync(
            Guid boothId,
            DateTime date,
            CancellationToken cancellationToken = default);

        Task<Rental?> GetNearestRentalAfterAsync(
            Guid boothId,
            DateTime date,
            CancellationToken cancellationToken = default);
    }
}

