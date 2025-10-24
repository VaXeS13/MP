using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using MP.Domain.Rentals;

namespace MP.Domain.Booths
{
    public class Booth : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; private set; }
        public Guid OrganizationalUnitId { get; private set; }
        public string Number { get; private set; } = null!;
        public BoothStatus Status { get; private set; }

        /// <summary>
        /// Stores the status before entering maintenance mode, to allow restoration
        /// </summary>
        public BoothStatus? StatusBeforeMaintenance { get; private set; }

        [Obsolete("Use PricingPeriods instead. This property is kept for backward compatibility.")]
        public decimal PricePerDay { get; private set; }

        /// <summary>
        /// Collection of pricing periods for flexible pricing (e.g., 1 day = 5 PLN, 7 days = 30 PLN)
        /// </summary>
        public List<PricingPeriod> PricingPeriods { get; private set; } = new();

        // Konstruktor dla EF Core
        private Booth() { }

        public Booth(
           Guid id,
           string number,
           decimal pricePerDay,
           Guid organizationalUnitId,
           Guid? tenantId = null
       ) : base(id)
        {
            TenantId = tenantId;
            OrganizationalUnitId = organizationalUnitId;
            SetNumber(number);
            SetPricePerDay(pricePerDay);
            Status = BoothStatus.Available;
        }

        /// <summary>
        /// New constructor for multi-period pricing
        /// </summary>
        public Booth(
           Guid id,
           string number,
           List<PricingPeriod> pricingPeriods,
           Guid organizationalUnitId,
           Guid? tenantId = null
       ) : base(id)
        {
            TenantId = tenantId;
            OrganizationalUnitId = organizationalUnitId;
            SetNumber(number);
            SetPricingPeriods(pricingPeriods);
            Status = BoothStatus.Available;
        }

        // Pozostała logika bez zmian...
        public void SetNumber(string number)
        {
            if (string.IsNullOrWhiteSpace(number))
                throw new BusinessException("BOOTH_NUMBER_REQUIRED");

            if (number.Length > 10)
                throw new BusinessException("BOOTH_NUMBER_TOO_LONG");

            Number = number.Trim().ToUpper();
        }


        public void SetPricePerDay(decimal price)
        {
            if (price <= 0)
                throw new BusinessException("BOOTH_PRICE_MUST_BE_POSITIVE");

            PricePerDay = price;
        }

        public void MarkAsReserved()
        {
            // Status can be changed to Reserved regardless of current status
            // Period-based availability is validated separately in RentalManager
            Status = BoothStatus.Reserved;
        }

        public void MarkAsRented()
        {
            // Status can be changed to Rented regardless of current status
            // Period-based availability is validated separately in RentalManager
            Status = BoothStatus.Rented;
        }

        public void MarkAsAvailable()
        {
            Status = BoothStatus.Available;
        }

        public void MarkAsMaintenance()
        {
            if (Status == BoothStatus.Rented || Status == BoothStatus.Reserved)
                throw new BusinessException("CANNOT_MAINTENANCE_RENTED_OR_RESERVED_BOOTH");

            // Save current status before entering maintenance (unless already in maintenance)
            if (Status != BoothStatus.Maintenance)
            {
                StatusBeforeMaintenance = Status;
            }

            Status = BoothStatus.Maintenance;
        }

        /// <summary>
        /// Restores booth status from maintenance to its previous status
        /// </summary>
        public void RestoreFromMaintenance()
        {
            if (Status != BoothStatus.Maintenance)
                throw new BusinessException("BOOTH_NOT_IN_MAINTENANCE");

            // Restore to previous status, or Available if no previous status was saved
            Status = StatusBeforeMaintenance ?? BoothStatus.Available;
            StatusBeforeMaintenance = null;
        }

        public bool IsAvailable()
        {
            return Status == BoothStatus.Available;
        }

        /// <summary>
        /// Set pricing periods for this booth (replaces old periods)
        /// </summary>
        public void SetPricingPeriods(List<PricingPeriod> periods)
        {
            if (periods == null || periods.Count == 0)
                throw new BusinessException("BOOTH_PRICING_PERIODS_REQUIRED");

            // Validate unique days
            var distinctDays = periods.Select(p => p.Days).Distinct().Count();
            if (distinctDays != periods.Count)
                throw new BusinessException("BOOTH_PRICING_PERIODS_MUST_BE_UNIQUE");

            // Validate all prices are positive
            if (periods.Any(p => p.PricePerPeriod <= 0))
                throw new BusinessException("BOOTH_PRICING_PERIOD_PRICE_MUST_BE_POSITIVE");

            // For EF Core OwnsMany: Clear and re-add to trigger change tracking
            PricingPeriods.RemoveAll(_ => true);
            foreach (var period in periods)
            {
                PricingPeriods.Add(period);
            }

            // Update legacy PricePerDay for backward compatibility (use 1-day period if exists)
            var oneDayPeriod = periods.FirstOrDefault(p => p.Days == 1);
            if (oneDayPeriod != null)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                PricePerDay = oneDayPeriod.PricePerPeriod;
#pragma warning restore CS0618 // Type or member is obsolete
            }
            else
            {
                // If no 1-day period, calculate from smallest period
                var smallestPeriod = periods.OrderBy(p => p.Days).First();
#pragma warning disable CS0618 // Type or member is obsolete
                PricePerDay = smallestPeriod.PricePerPeriod / smallestPeriod.Days;
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        /// <summary>
        /// Calculate total price for given number of days using greedy algorithm.
        /// Example: 16 days with periods [1d=1zł, 7d=6zł] => 2×7d + 2×1d = 14zł
        /// </summary>
        /// <param name="totalDays">Total number of rental days</param>
        /// <returns>Breakdown with total price and period usage</returns>
        public PriceCalculationResult CalculatePrice(int totalDays)
        {
            if (totalDays <= 0)
                throw new BusinessException("RENTAL_DAYS_MUST_BE_POSITIVE")
                    .WithData("days", totalDays);

            if (PricingPeriods == null || PricingPeriods.Count == 0)
                throw new BusinessException("BOOTH_NO_PRICING_PERIODS_DEFINED");

            var result = new PriceCalculationResult();
            var remainingDays = totalDays;

            // Sort periods by days descending (greedy: use largest periods first)
            var sortedPeriods = PricingPeriods.OrderByDescending(p => p.Days).ToList();

            foreach (var period in sortedPeriods)
            {
                var count = remainingDays / period.Days;
                if (count > 0)
                {
                    result.AddPeriodUsage(period.Days, count, period.PricePerPeriod);
                    remainingDays -= count * period.Days;
                }

                if (remainingDays == 0)
                    break;
            }

            // If there are remaining days and no suitable period, use the smallest period
            if (remainingDays > 0)
            {
                var smallestPeriod = sortedPeriods.OrderBy(p => p.Days).First();
                var count = (int)Math.Ceiling((decimal)remainingDays / smallestPeriod.Days);
                result.AddPeriodUsage(smallestPeriod.Days, count, smallestPeriod.PricePerPeriod);
            }

            return result;
        }
    }

    /// <summary>
    /// Result of price calculation with breakdown
    /// </summary>
    public class PriceCalculationResult
    {
        public decimal TotalPrice { get; private set; }
        public List<PeriodUsage> Breakdown { get; private set; } = new();

        public void AddPeriodUsage(int days, int count, decimal pricePerPeriod)
        {
            Breakdown.Add(new PeriodUsage
            {
                Days = days,
                Count = count,
                PricePerPeriod = pricePerPeriod,
                Subtotal = count * pricePerPeriod
            });

            TotalPrice += count * pricePerPeriod;
        }

        public class PeriodUsage
        {
            public int Days { get; set; }
            public int Count { get; set; }
            public decimal PricePerPeriod { get; set; }
            public decimal Subtotal { get; set; }

            public override string ToString()
            {
                return $"{Count}×{Days} day(s) ({PricePerPeriod:C} each) = {Subtotal:C}";
            }
        }

        public override string ToString()
        {
            var breakdown = string.Join(" + ", Breakdown.Select(b => b.ToString()));
            return $"{breakdown} = {TotalPrice:C}";
        }
    }
}