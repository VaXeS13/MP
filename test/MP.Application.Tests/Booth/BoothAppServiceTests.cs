using Volo.Abp.Uow;
﻿using MP.Booths;
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
        [UnitOfWork]
        public async Task Should_Create_Valid_Booth()
        {
            // Arrange
            var createDto = new CreateBoothDto
            {
                Number = "TEST01",
                PricePerDay = 25.00m
            };

            // Act
            var result = await _boothAppService.CreateAsync(createDto);

            // Assert
            result.ShouldNotBeNull();
            result.Number.ShouldBe("TEST01");
            result.Status.ShouldBe(BoothStatus.Available);
            result.PricePerDay.ShouldBe(25.00m);
        }

        [Fact]
        [UnitOfWork]
        public async Task Should_Not_Create_Booth_With_Empty_Number()
        {
            // Arrange
            var createDto = new CreateBoothDto
            {
                Number = "",
                PricePerDay = 25.00m
            };

            // Act & Assert
            var exception = await Should.ThrowAsync<AbpValidationException>(
                async () => await _boothAppService.CreateAsync(createDto));
        }

        [Fact]
        [UnitOfWork]
        public async Task Should_Not_Create_Booth_With_Duplicate_Number()
        {
            // Arrange - Utwórz pierwsze stanowisko
            var booth1 = new MP.Domain.Booths.Booth(Guid.NewGuid(), "DUPLICATE", 25.00m);
            await _boothRepository.InsertAsync(booth1);

            var createDto = new CreateBoothDto
            {
                Number = "DUPLICATE",
                PricePerDay = 30.00m
            };

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                async () => await _boothAppService.CreateAsync(createDto));

            exception.Code.ShouldBe("BOOTH_NUMBER_ALREADY_EXISTS");
        }

        [Fact]
        [UnitOfWork]
        public async Task Should_Get_Available_Booths_Only()
        {
            // Arrange
            var availableBooth = new MP.Domain.Booths.Booth(Guid.NewGuid(), "AVAIL01", 25.00m);
            var rentedBooth = new MP.Domain.Booths.Booth(Guid.NewGuid(), "RENTED01", 25.00m);
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
        [UnitOfWork]
        public async Task Should_Create_Booth_With_Correct_Price()
        {
            // Arrange
            var createDto1 = new CreateBoothDto
            {
                Number = "SELF01",
                PricePerDay = 25.00m
            };

            var createDto2 = new CreateBoothDto
            {
                Number = "SHOP01",
                PricePerDay = 35.00m
            };

            // Act
            var booth1 = await _boothAppService.CreateAsync(createDto1);
            var booth2 = await _boothAppService.CreateAsync(createDto2);

            // Assert
            booth1.PricePerDay.ShouldBe(25.00m);
            booth2.PricePerDay.ShouldBe(35.00m);
        }

        [Fact]
        [UnitOfWork]
        public async Task Should_Change_Booth_Status()
        {
            // Arrange
            var booth = await _boothAppService.CreateAsync(new CreateBoothDto
            {
                Number = "STATUS01",
                PricePerDay = 25.00m
            });

            // Act
            var updatedBooth = await _boothAppService.ChangeStatusAsync(booth.Id, BoothStatus.Maintenance);

            // Assert
            updatedBooth.Status.ShouldBe(BoothStatus.Maintenance);
        }
    }
}