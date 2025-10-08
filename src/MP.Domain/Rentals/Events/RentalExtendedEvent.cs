using MP.Domain.Rentals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Events;

namespace MP.Domain.Rentals.Events
{
    public class RentalExtendedEvent : EntityUpdatedEventData<Rental>
    {
        public decimal AdditionalCost { get; }

        public RentalExtendedEvent(Rental rental, decimal additionalCost) : base(rental)
        {
            AdditionalCost = additionalCost;
        }
    }
}
