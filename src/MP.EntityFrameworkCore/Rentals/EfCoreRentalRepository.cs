using Microsoft.EntityFrameworkCore;
using MP.Domain.Rentals;
using MP.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace MP.Rentals
{
    public class EfCoreRentalRepository : EfCoreRepository<MPDbContext, Rental, Guid>, IRentalRepository
    {
        public EfCoreRentalRepository(IDbContextProvider<MPDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<List<Rental>> GetRentalsForUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Rentals
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .Include(r => r.User)
                .Include(r => r.Booth)
                .OrderByDescending(r => r.CreationTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Rental>> GetActiveRentalsAsync(CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Rentals
                .AsNoTracking()
                .Where(r => r.Status == RentalStatus.Active || r.Status == RentalStatus.Extended)
                .Include(r => r.User)
                .Include(r => r.Booth)
                .OrderBy(r => r.Period.EndDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Rental>> GetExpiredRentalsAsync(
            DateTime beforeDate,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Rentals
                .AsNoTracking()
                .Where(r => (r.Status == RentalStatus.Active || r.Status == RentalStatus.Extended) &&
                           r.Period.EndDate < beforeDate.Date)
                .Include(r => r.User)
                .Include(r => r.Booth)
                .ToListAsync(cancellationToken);
        }

        public async Task<Rental?> GetRentalWithItemsAsync(
            Guid rentalId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Rentals
                .AsNoTracking()
                .Where(r => r.Id == rentalId)
                .Include(r => r.User)
                .Include(r => r.Booth)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> HasActiveRentalForBoothAsync(
            Guid boothId,
            DateTime startDate,
            DateTime endDate,
            Guid? excludeRentalId = null,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var query = dbContext.Rentals
                .AsNoTracking()
                .Where(r => r.BoothId == boothId &&
                           (r.Status == RentalStatus.Draft || r.Status == RentalStatus.Active || r.Status == RentalStatus.Extended) &&
                           r.Period.StartDate <= endDate.Date &&
                           r.Period.EndDate >= startDate.Date);

            if (excludeRentalId.HasValue)
            {
                query = query.Where(r => r.Id != excludeRentalId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        }

        public async Task<List<Rental>> GetRentalsForBoothAsync(
            Guid boothId,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Rentals
                .AsNoTracking()
                .Where(r => r.BoothId == boothId)
                .OrderBy(r => r.Period.StartDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<decimal> GetTotalRevenueAsync(
            DateTime fromDate,
            DateTime toDate,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();

            // Przychody z wynajęcia stanowisk
            var rentalRevenue = await dbContext.Rentals
                .AsNoTracking()
                .Where(r => r.Status != RentalStatus.Cancelled &&
                           r.Payment.PaidDate != null &&
                           r.Payment.PaidDate >= fromDate &&
                           r.Payment.PaidDate <= toDate)
                .SumAsync(r => r.Payment.PaidAmount, cancellationToken);

            // Prowizje ze sprzedaży - using ItemSheetItems instead of RentalItems
            var commissionRevenue = await dbContext.ItemSheetItems
                .AsNoTracking()
                .Where(isi => isi.Status == MP.Domain.Items.ItemSheetItemStatus.Sold &&
                            isi.SoldAt != null &&
                            isi.SoldAt >= fromDate &&
                            isi.SoldAt <= toDate &&
                            isi.Item != null)
                .SumAsync(isi => isi.Item.Price * (isi.CommissionPercentage / 100), cancellationToken);

            return rentalRevenue + commissionRevenue;
        }

        public async Task<Rental?> GetNearestRentalBeforeAsync(
            Guid boothId,
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Rentals
                .AsNoTracking()
                .Where(r => r.BoothId == boothId &&
                           (r.Status == RentalStatus.Draft ||
                            r.Status == RentalStatus.Active ||
                            r.Status == RentalStatus.Extended) &&
                           r.Period.EndDate < date.Date)
                .OrderByDescending(r => r.Period.EndDate)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Rental?> GetNearestRentalAfterAsync(
            Guid boothId,
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Rentals
                .AsNoTracking()
                .Where(r => r.BoothId == boothId &&
                           (r.Status == RentalStatus.Draft ||
                            r.Status == RentalStatus.Active ||
                            r.Status == RentalStatus.Extended) &&
                           r.Period.StartDate > date.Date)
                .OrderBy(r => r.Period.StartDate)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}