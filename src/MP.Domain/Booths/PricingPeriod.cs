using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Values;

namespace MP.Domain.Booths
{
    /// <summary>
    /// Value object representing a pricing period for booth rental.
    /// Example: 7 days for 6 PLN per period
    /// </summary>
    public class PricingPeriod : ValueObject
    {
        /// <summary>
        /// Number of days in this pricing period (e.g., 1, 3, 7, 14, 30)
        /// </summary>
        public int Days { get; private set; }

        /// <summary>
        /// Price for this complete period (not per day)
        /// </summary>
        public decimal PricePerPeriod { get; private set; }

        /// <summary>
        /// ID of the booth this pricing period belongs to (for EF Core)
        /// </summary>
        public Guid BoothId { get; private set; }

        // For EF Core
        private PricingPeriod() { }

        public PricingPeriod(int days, decimal pricePerPeriod, Guid boothId)
        {
            if (days <= 0)
                throw new BusinessException("PRICING_PERIOD_DAYS_MUST_BE_POSITIVE")
                    .WithData("days", days);

            if (pricePerPeriod <= 0)
                throw new BusinessException("PRICING_PERIOD_PRICE_MUST_BE_POSITIVE")
                    .WithData("price", pricePerPeriod);

            Days = days;
            PricePerPeriod = pricePerPeriod;
            BoothId = boothId;
        }

        /// <summary>
        /// Calculate effective price per day for comparison
        /// </summary>
        public decimal GetPricePerDay()
        {
            return PricePerPeriod / Days;
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Days;
            yield return PricePerPeriod;
            yield return BoothId;
        }

        public override string ToString()
        {
            return $"{Days} day(s) = {PricePerPeriod:C}";
        }
    }
}
