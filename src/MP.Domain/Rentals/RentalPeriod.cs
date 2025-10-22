using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Values;
using Volo.Abp;

namespace MP.Domain.Rentals
{
    public class RentalPeriod : ValueObject
    {
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }

        private RentalPeriod() { } // Dla EF Core

        public RentalPeriod(DateTime startDate, DateTime endDate)
        {
            ValidatePeriod(startDate, endDate);
            StartDate = startDate.Date; // Tylko data, bez godziny
            EndDate = endDate.Date;
        }

        private void ValidatePeriod(DateTime startDate, DateTime endDate)
        {
            if (startDate.Date < DateTime.Today)
                throw new BusinessException("RENTAL_START_DATE_CANNOT_BE_IN_PAST");

            if (endDate <= startDate)
                throw new BusinessException("RENTAL_END_DATE_MUST_BE_AFTER_START");

            // Calculate days count using parameters instead of properties (which are not set yet)
            var daysCount = (endDate.Date - startDate.Date).Days + 1;
            if (daysCount < 1)
                throw new BusinessException("RENTAL_PERIOD_MUST_BE_AT_LEAST_ONE_DAY");
        }

        public int GetDaysCount()
        {
            return (EndDate - StartDate).Days + 1; // +1 bo liczymy dzień rozpoczęcia
        }

        public bool OverlapsWith(RentalPeriod other)
        {
            return StartDate <= other.EndDate && EndDate >= other.StartDate;
        }

        public bool HasGapBefore(RentalPeriod previousRental)
        {
            return StartDate > previousRental.EndDate.AddDays(1);
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return StartDate;
            yield return EndDate;
        }

        public static RentalPeriod Create(DateTime startDate, int daysCount)
        {
            if (daysCount < 1)
                throw new BusinessException("RENTAL_PERIOD_MUST_BE_AT_LEAST_ONE_DAY");

            return new RentalPeriod(startDate, startDate.AddDays(daysCount - 1));
        }
    }
}
