using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;
using Volo.Abp;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.Booths
{
    public class BoothManager : DomainService
    {
        private readonly IBoothRepository _boothRepository;
        private readonly ICurrentTenant _currentTenant;  // ← DODAJ

        public BoothManager(
            IBoothRepository boothRepository,
            ICurrentTenant currentTenant)  // ← DODAJ
        {
            _boothRepository = boothRepository;
            _currentTenant = currentTenant;  // ← DODAJ
        }

        public async Task<Booth> CreateAsync(
            string number,
            decimal pricePerDay,
            Currency currency = Currency.PLN)
        {
            // Sprawdź czy numer jest unikalny w tym tenant
            if (!await _boothRepository.IsNumberUniqueAsync(number))
            {
                throw new BusinessException("BOOTH_NUMBER_ALREADY_EXISTS")
                    .WithData("number", number);
            }

            var booth = new Booth(
                GuidGenerator.Create(),
                number,
                pricePerDay,
                currency,
                _currentTenant.Id  // ← PRZEKAŻ TENANT ID
            );

            return booth;
        }

        public async Task ChangeNumberAsync(Booth booth, string newNumber)
        {
            if (!await _boothRepository.IsNumberUniqueAsync(newNumber, booth.Id))
            {
                throw new BusinessException("BOOTH_NUMBER_ALREADY_EXISTS")
                    .WithData("number", newNumber);
            }

            booth.SetNumber(newNumber);
        }
    }
}