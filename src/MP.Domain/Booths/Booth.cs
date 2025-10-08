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
        public string Number { get; private set; } = null!;
        public BoothStatus Status { get; private set; }
        public decimal PricePerDay { get; private set; }
        public Currency Currency { get; private set; }

        // Navigation property for current rentals
        public ICollection<Rental> Rentals { get; set; } = new List<Rental>();

        // Konstruktor dla EF Core
        private Booth() { }

        public Booth(
           Guid id,
           string number,
           decimal pricePerDay,
           Currency currency = Currency.PLN,
           Guid? tenantId = null
       ) : base(id)
        {
            TenantId = tenantId;
            SetNumber(number);
            SetPricePerDay(pricePerDay);
            SetCurrency(currency);
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

        public void SetCurrency(Currency currency)
        {
            Currency = currency;
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

        public void MarkAsMaintenace()
        {
            if (Status == BoothStatus.Rented || Status == BoothStatus.Reserved)
                throw new BusinessException("CANNOT_MAINTENANCE_RENTED_OR_RESERVED_BOOTH");

            Status = BoothStatus.Maintenance;
        }

        public bool IsAvailable()
        {
            return Status == BoothStatus.Available;
        }

    }
}