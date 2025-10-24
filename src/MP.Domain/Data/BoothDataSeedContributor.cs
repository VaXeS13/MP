using MP.Domain.Booths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;

namespace MP.Domain.Data
{
    public class BoothDataSeedContributor : IDataSeedContributor, ITransientDependency
    {
        private readonly IBoothRepository _boothRepository;
        private readonly BoothManager _boothManager;

        public BoothDataSeedContributor(
            IBoothRepository boothRepository,
            BoothManager boothManager)
        {
            _boothRepository = boothRepository;
            _boothManager = boothManager;
        }

        public async Task SeedAsync(DataSeedContext context)
        {
            if (await _boothRepository.GetCountAsync() > 0)
            {
                return; // Już mamy stanowiska
            }

            // TODO: OU-12 - Seedowanie będzie aktualizowane gdy dodamy OrganizationalUnits
            // Na razie używamy tymczasowego OrganizationalUnitId
            var defaultOrganizationalUnitId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            // Dodaj przykładowe stanowiska
            var testBooths = new[]
            {
                new { Number = "A01", Price = 25.00m },
            };

            foreach (var testBooth in testBooths)
            {
                var booth = new Booth(
                    Guid.NewGuid(),
                    testBooth.Number,
                    testBooth.Price,
                    defaultOrganizationalUnitId
                );

                await _boothRepository.InsertAsync(booth, autoSave: true);
            }
        }
    }
}
