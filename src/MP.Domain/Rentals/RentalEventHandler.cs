using MP.Domain.Booths;
using MP.Domain.Rentals.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace MP.Domain.Rentals
{
    public class RentalEventHandler :
        ILocalEventHandler<RentalConfirmedEvent>,
        ILocalEventHandler<RentalCompletedEvent>,
        ILocalEventHandler<RentalCancelledEvent>,
        ITransientDependency
    {
        private readonly IBoothRepository _boothRepository;

        public RentalEventHandler(IBoothRepository boothRepository)
        {
            _boothRepository = boothRepository;
        }

        public async Task HandleEventAsync(RentalConfirmedEvent eventData)
        {
            // Gdy wynajęcie jest potwierdzone, oznacz stanowisko jako wynajęte
            var booth = await _boothRepository.GetAsync(eventData.Entity.BoothId);
            booth.MarkAsRented();
            await _boothRepository.UpdateAsync(booth);
        }

        public async Task HandleEventAsync(RentalCompletedEvent eventData)
        {
            // Gdy wynajęcie się kończy, zwolnij stanowisko
            var booth = await _boothRepository.GetAsync(eventData.Entity.BoothId);
            booth.MarkAsAvailable();
            await _boothRepository.UpdateAsync(booth);
        }

        public async Task HandleEventAsync(RentalCancelledEvent eventData)
        {
            // Gdy wynajęcie jest anulowane, zwolnij stanowisko
            var booth = await _boothRepository.GetAsync(eventData.Entity.BoothId);
            booth.MarkAsAvailable();
            await _boothRepository.UpdateAsync(booth);
        }
    }
}
