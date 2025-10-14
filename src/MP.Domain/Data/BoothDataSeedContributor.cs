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

            // Dodaj przykładowe stanowiska
            var testBooths = new[]
            {
                new { Number = "A01", Price = 25.00m },
            };

            foreach (var testBooth in testBooths)
            {
                var booth = await _boothManager.CreateAsync(
                    testBooth.Number,
                    testBooth.Price
                );

                await _boothRepository.InsertAsync(booth, autoSave: true);
            }
        }
    }
}
