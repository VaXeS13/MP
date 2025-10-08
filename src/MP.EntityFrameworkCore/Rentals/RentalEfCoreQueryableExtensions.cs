using MP.Domain.Rentals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP.Rentals
{
    public static class RentalEfCoreQueryableExtensions
    {
        public static IQueryable<Rental> WhereActive(this IQueryable<Rental> queryable)
        {
            return queryable.Where(x => x.Status == RentalStatus.Active || x.Status == RentalStatus.Extended);
        }

        public static IQueryable<Rental> WhereUser(this IQueryable<Rental> queryable, Guid userId)
        {
            return queryable.Where(x => x.UserId == userId);
        }

        public static IQueryable<Rental> WhereBooth(this IQueryable<Rental> queryable, Guid boothId)
        {
            return queryable.Where(x => x.BoothId == boothId);
        }

        public static IQueryable<Rental> WherePeriod(this IQueryable<Rental> queryable, DateTime fromDate, DateTime toDate)
        {
            return queryable.Where(x => x.Period.StartDate <= toDate && x.Period.EndDate >= fromDate);
        }

        public static IQueryable<Rental> WhereExpiring(this IQueryable<Rental> queryable, DateTime beforeDate)
        {
            return queryable.WhereActive()
                           .Where(x => x.Period.EndDate <= beforeDate);
        }

        public static IQueryable<Rental> WhereOverdue(this IQueryable<Rental> queryable, DateTime currentDate)
        {
            return queryable.WhereActive()
                           .Where(x => x.Period.EndDate < currentDate);
        }
    }
}