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
        private static readonly Guid TestOrganizationalUnitId = new Guid("00000000-0000-0000-0000-000000000010");

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
            var boothNumber = $"BOOTH{Guid.NewGuid().ToString().Substring(0, 5)}".ToUpper();
            var createDto = new CreateBoothDto
            {
                OrganizationalUnitId = TestOrganizationalUnitId,
                Number = boothNumber,
                PricePerDay = 25.00m
            };

            // Act
            var result = await _boothAppService.CreateAsync(createDto);

            // Assert
            result.ShouldNotBeNull();
            result.Number.ShouldBe(boothNumber);
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
                OrganizationalUnitId = TestOrganizationalUnitId,
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
            // Arrange - Create first booth with specific number
            var duplicateNumber = $"DUP{Guid.NewGuid().ToString().Substring(0, 5)}".ToUpper();
            var booth1 = new MP.Domain.Booths.Booth(Guid.NewGuid(), duplicateNumber, 25.00m, TestOrganizationalUnitId);
            await _boothRepository.InsertAsync(booth1);

            var createDto = new CreateBoothDto
            {
                OrganizationalUnitId = TestOrganizationalUnitId,
                Number = duplicateNumber,
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
            var availableNumber = $"AVAIL{Guid.NewGuid().ToString().Substring(0, 4)}".ToUpper();
            var rentedNumber = $"RENTED{Guid.NewGuid().ToString().Substring(0, 3)}".ToUpper();
            var availableBooth = new MP.Domain.Booths.Booth(Guid.NewGuid(), availableNumber, 25.00m, TestOrganizationalUnitId);
            var rentedBooth = new MP.Domain.Booths.Booth(Guid.NewGuid(), rentedNumber, 25.00m, TestOrganizationalUnitId);
            rentedBooth.MarkAsRented();

            await _boothRepository.InsertAsync(availableBooth);
            await _boothRepository.InsertAsync(rentedBooth);

            // Act
            var availableBooths = await _boothAppService.GetAvailableBoothsAsync();

            // Assert
            availableBooths.ShouldContain(b => b.Number == availableNumber);
            availableBooths.ShouldNotContain(b => b.Number == rentedNumber);
        }

        [Fact]
        [UnitOfWork]
        public async Task Should_Create_Booth_With_Correct_Price()
        {
            // Arrange
            var createDto1 = new CreateBoothDto
            {
                OrganizationalUnitId = TestOrganizationalUnitId,
                Number = $"SELF{Guid.NewGuid().ToString().Substring(0, 4)}".ToUpper(),
                PricePerDay = 25.00m
            };

            var createDto2 = new CreateBoothDto
            {
                OrganizationalUnitId = TestOrganizationalUnitId,
                Number = $"SHOP{Guid.NewGuid().ToString().Substring(0, 4)}".ToUpper(),
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
                OrganizationalUnitId = TestOrganizationalUnitId,
                Number = $"STAT{Guid.NewGuid().ToString().Substring(0, 5)}".ToUpper(),
                PricePerDay = 25.00m
            });

            // Act
            var updatedBooth = await _boothAppService.ChangeStatusAsync(booth.Id, BoothStatus.Maintenance);

            // Assert
            updatedBooth.Status.ShouldBe(BoothStatus.Maintenance);
        }
    }
}