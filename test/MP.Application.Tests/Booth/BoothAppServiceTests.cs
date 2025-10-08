using MP.Booths;
using MP.Domain.Booths;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Validation;
using Volo.Abp;
using Xunit;
namespace MP.Booth
{
    public class BoothAppServiceTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly IBoothAppService _boothAppService;
        private readonly IBoothRepository _boothRepository;

        public BoothAppServiceTests()
        {
            _boothAppService = GetRequiredService<IBoothAppService>();
            _boothRepository = GetRequiredService<IBoothRepository>();
        }

        [Fact]
        public async Task Should_Create_Valid_Booth()
        {
            // Arrange
            var createDto = new CreateBoothDto
            {
                Number = "TEST01",
                Type = BoothType.SelfPricing,
                PricePerDay = 25.00m
            };

            // Act
            var result = await _boothAppService.CreateAsync(createDto);

            // Assert
            result.ShouldNotBeNull();
            result.Number.ShouldBe("TEST01");
            result.Type.ShouldBe(BoothType.SelfPricing);
            result.Status.ShouldBe(BoothStatus.Available);
            result.CommissionPercentage.ShouldBe(10m); // SelfPricing = 10%
        }

        [Fact]
        public async Task Should_Not_Create_Booth_With_Empty_Number()
        {
            // Arrange
            var createDto = new CreateBoothDto
            {
                Number = "",
                Type = BoothType.SelfPricing,
                PricePerDay = 25.00m
            };

            // Act & Assert
            var exception = await Should.ThrowAsync<AbpValidationException>(
                async () => await _boothAppService.CreateAsync(createDto));
        }

        [Fact]
        public async Task Should_Not_Create_Booth_With_Duplicate_Number()
        {
            // Arrange - Utwórz pierwsze stanowisko
            var booth1 = new MP.Domain.Booths.Booth(Guid.NewGuid(), "DUPLICATE", BoothType.SelfPricing, 25.00m);
            await _boothRepository.InsertAsync(booth1);

            var createDto = new CreateBoothDto
            {
                Number = "DUPLICATE",
                Type = BoothType.SelfPricing,
                PricePerDay = 30.00m
            };

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                async () => await _boothAppService.CreateAsync(createDto));

            exception.Code.ShouldBe("BOOTH_NUMBER_ALREADY_EXISTS");
        }

        [Fact]
        public async Task Should_Get_Available_Booths_Only()
        {
            // Arrange
            var availableBooth = new MP.Domain.Booths.Booth(Guid.NewGuid(), "AVAIL01", BoothType.SelfPricing, 25.00m);
            var rentedBooth = new MP.Domain.Booths.Booth(Guid.NewGuid(), "RENTED01", BoothType.SelfPricing, 25.00m);
            rentedBooth.MarkAsRented();

            await _boothRepository.InsertAsync(availableBooth);
            await _boothRepository.InsertAsync(rentedBooth);

            // Act
            var availableBooths = await _boothAppService.GetAvailableBoothsAsync();

            // Assert
            availableBooths.ShouldContain(b => b.Number == "AVAIL01");
            availableBooths.ShouldNotContain(b => b.Number == "RENTED01");
        }

        [Fact]
        public async Task Should_Calculate_Commission_Correctly()
        {
            // Arrange
            var selfPricingBooth = await _boothAppService.CreateAsync(new CreateBoothDto
            {
                Number = "SELF01",
                Type = BoothType.SelfPricing,
                PricePerDay = 25.00m
            });

            var shopPricingBooth = await _boothAppService.CreateAsync(new CreateBoothDto
            {
                Number = "SHOP01",
                Type = BoothType.ShopPricing,
                PricePerDay = 35.00m
            });

            // Assert
            selfPricingBooth.CommissionPercentage.ShouldBe(10m);
            shopPricingBooth.CommissionPercentage.ShouldBe(30m);
        }

        [Fact]
        public async Task Should_Change_Booth_Status()
        {
            // Arrange
            var booth = await _boothAppService.CreateAsync(new CreateBoothDto
            {
                Number = "STATUS01",
                Type = BoothType.SelfPricing,
                PricePerDay = 25.00m
            });

            // Act
            var updatedBooth = await _boothAppService.ChangeStatusAsync(booth.Id, BoothStatus.Maintenance);

            // Assert
            updatedBooth.Status.ShouldBe(BoothStatus.Maintenance);
        }
    }
}