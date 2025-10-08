using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MP.Domain.Booths;

namespace MP.Booths
{
    public static class BoothEfCoreQueryableExtensions
    {
        public static IQueryable<Booth> WhereAvailable(this IQueryable<Booth> queryable)
        {
            return queryable.Where(x => x.Status == BoothStatus.Available);
        }


        public static IQueryable<Booth> WhereNumber(this IQueryable<Booth> queryable, string number)
        {
            return queryable.Where(x => x.Number.Contains(number.ToUpper()));
        }
    }
}
