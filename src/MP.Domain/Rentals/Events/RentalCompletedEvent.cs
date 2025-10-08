using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Events;

namespace MP.Domain.Rentals.Events
{
    public class RentalCompletedEvent : EntityUpdatedEventData<Rental>
    {
        public RentalCompletedEvent(Rental rental) : base(rental)
        {
        }
    }
}
