using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MP.Domain.Booths;
using MP.Domain.BoothTypes;
using MP.Domain.Rentals;
using MP.Rentals;
using NSubstitute;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Domain.Tests.Rentals
{
    public class RentalManagerTests : MPDomainTestBase<MPDomainTestModule>
    {
        private readonly IRentalRepository _rentalRepository;
        private readonly IBoothRepository _boothRepository;
        private readonly IBoothTypeRepository _boothTypeRepository;
        private readonly RentalManager _rentalManager;

        // Use known test user IDs that are seeded in MPDomainTestModule
        private static readonly Guid TestUserId1 = new Guid("00000000-0000-0000-0000-000000000001");
        private static readonly Guid TestUserId2 = new Guid("00000000-0000-0000-0000-000000000002");
        private static readonly Guid DefaultOrganizationalUnitId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        public RentalManagerTests()
        {
            _rentalRepository = GetRequiredService<IRentalRepository>();
            _boothRepository = GetRequiredService<IBoothRepository>();
            _boothTypeRepository = GetRequiredService<IBoothTypeRepository>();
            _rentalManager = GetRequiredService<RentalManager>();
        }

        [Fact]
        public async Task CreateRentalAsync_Should_Create_Valid_Rental_With_Correct_Status()
        {
            // Arrange
            var userId = TestUserId1;
            var boothNumber = $"B{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            var booth = new Booth(Guid.NewGuid(), boothNumber, 100m, DefaultOrganizationalUnitId);
            await _boothRepository.InsertAsync(booth);

            var boothType = new BoothType(Guid.NewGuid(), "Standard", "Standard booth", 10m, DefaultOrganizationalUnitId);
            await _boothTypeRepository.InsertAsync(boothType);

            var startDate = DateTime.Today.AddDays(7);
            var endDate = startDate.AddDays(10);

            // Act
            var rental = await _rentalManager.CreateRentalAsync(
                userId,
                booth.Id,
                boothType.Id,
                startDate,
                endDate
            );

            // Assert
            rental.ShouldNotBeNull();
            rental.UserId.ShouldBe(userId);
            rental.BoothId.ShouldBe(booth.Id);
            rental.BoothTypeId.ShouldBe(boothType.Id);
            rental.Status.ShouldBe(RentalStatus.Draft);
            rental.Period.StartDate.ShouldBe(startDate);
            rental.Period.EndDate.ShouldBe(endDate);
            rental.Currency.ShouldBe(Currency.PLN);
        }

        [Fact]
        public async Task CreateRentalAsync_Should_Calculate_Correct_Total_Cost()
        {
            // Arrange
            var userId = TestUserId1;
            var dailyPrice = 100m;
            var boothNumber = $"B{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            var booth = new Booth(Guid.NewGuid(), boothNumber, dailyPrice, DefaultOrganizationalUnitId);
            await _boothRepository.InsertAsync(booth);

            var boothType = new BoothType(Guid.NewGuid(), "Standard", "Standard booth", 10m, DefaultOrganizationalUnitId);
            await _boothTypeRepository.InsertAsync(boothType);

            var startDate = DateTime.Today.AddDays(7);
            var endDate = startDate.AddDays(6); // 7 days total (minimum)

            // Act
            var rental = await _rentalManager.CreateRentalAsync(
                userId,
                booth.Id,
                boothType.Id,
                startDate,
                endDate
            );

            // Assert
            var expectedCost = dailyPrice * 7; // 7 days
            rental.Payment.TotalAmount.ShouldBe(expectedCost);
        }

        [Fact]
        public async Task CreateRentalAsync_Should_Use_Custom_Daily_Rate_If_Provided()
        {
            // Arrange
            var userId = TestUserId1;
            var boothDailyPrice = 100m;
            var customDailyRate = 150m;

            var boothNumber = $"B{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            var booth = new Booth(Guid.NewGuid(), boothNumber, boothDailyPrice, DefaultOrganizationalUnitId);
            await _boothRepository.InsertAsync(booth);

            var boothType = new BoothType(Guid.NewGuid(), "Premium", "Premium booth", 10m, DefaultOrganizationalUnitId);
            await _boothTypeRepository.InsertAsync(boothType);

            var startDate = DateTime.Today.AddDays(7);
            var endDate = startDate.AddDays(6);

            // Act
            var rental = await _rentalManager.CreateRentalAsync(
                userId,
                booth.Id,
                boothType.Id,
                startDate,
                endDate,
                customDailyRate
            );

            // Assert
            var expectedCost = customDailyRate * 7; // Should use custom rate
            rental.Payment.TotalAmount.ShouldBe(expectedCost);
        }

        [Fact]
        public async Task CreateRentalAsync_Should_Throw_When_Booth_Type_Not_Active()
        {
            // Arrange
            var userId = TestUserId1;
            var boothNumber = $"B{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            var booth = new Booth(Guid.NewGuid(), boothNumber, 100m, DefaultOrganizationalUnitId);
            await _boothRepository.InsertAsync(booth);

            var inactiveBoothType = new BoothType(Guid.NewGuid(), "Inactive", "Inactive booth", 10m, DefaultOrganizationalUnitId);
            inactiveBoothType.Deactivate(); // Assuming there's a method to deactivate
            await _boothTypeRepository.InsertAsync(inactiveBoothType);

            var startDate = DateTime.Today.AddDays(7);
            var endDate = startDate.AddDays(6);

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _rentalManager.CreateRentalAsync(
                    userId,
                    booth.Id,
                    inactiveBoothType.Id,
                    startDate,
                    endDate
                )
            );

            exception.Code.ShouldBe("BOOTH_TYPE_NOT_AVAILABLE");
        }

        [Fact]
        public async Task CreateRentalAsync_Should_Throw_When_Booth_In_Maintenance()
        {
            // Arrange
            var userId = TestUserId1;
            var boothNumber = $"B{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            var booth = new Booth(Guid.NewGuid(), boothNumber, 100m, DefaultOrganizationalUnitId);
            booth.MarkAsMaintenance();
            await _boothRepository.InsertAsync(booth);

            var boothType = new BoothType(Guid.NewGuid(), "Standard", "Standard booth", 10m, DefaultOrganizationalUnitId);
            await _boothTypeRepository.InsertAsync(boothType);

            var startDate = DateTime.Today.AddDays(7);
            var endDate = startDate.AddDays(6);

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _rentalManager.CreateRentalAsync(
                    userId,
                    booth.Id,
                    boothType.Id,
                    startDate,
                    endDate
                )
            );

            exception.Code.ShouldBe("BOOTH_IN_MAINTENANCE");
        }

        [Fact]
        public async Task CreateRentalAsync_Should_Throw_When_Booth_Already_Rented_In_Period()
        {
            // Arrange
            var userId1 = TestUserId1;
            var userId2 = TestUserId2;

            var boothNumber = $"B{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            var booth = new Booth(Guid.NewGuid(), boothNumber, 100m, DefaultOrganizationalUnitId);
            await _boothRepository.InsertAsync(booth);

            var boothType = new BoothType(Guid.NewGuid(), "Standard", "Standard booth", 10m, DefaultOrganizationalUnitId);
            await _boothTypeRepository.InsertAsync(boothType);

            var startDate1 = DateTime.Today.AddDays(7);
            var endDate1 = startDate1.AddDays(6);

            // Create first rental
            var rental1 = await _rentalManager.CreateRentalAsync(
                userId1,
                booth.Id,
                boothType.Id,
                startDate1,
                endDate1
            );
            await _rentalRepository.InsertAsync(rental1);

            var startDate2 = startDate1.AddDays(2); // Overlaps with first rental
            var endDate2 = startDate2.AddDays(6);

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _rentalManager.CreateRentalAsync(
                    userId2,
                    booth.Id,
                    boothType.Id,
                    startDate2,
                    endDate2
                )
            );

            exception.Code.ShouldBe("BOOTH_ALREADY_RENTED_IN_PERIOD");
        }

        [Fact]
        public async Task CreateRentalAsync_Should_Mark_Booth_As_Reserved()
        {
            // Arrange
            var userId = TestUserId1;
            var boothNumber = $"B{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            var booth = new Booth(Guid.NewGuid(), boothNumber, 100m, DefaultOrganizationalUnitId);
            booth.MarkAsAvailable(); // Ensure it's available
            await _boothRepository.InsertAsync(booth);

            var boothType = new BoothType(Guid.NewGuid(), "Standard", "Standard booth", 10m, DefaultOrganizationalUnitId);
            await _boothTypeRepository.InsertAsync(boothType);

            var startDate = DateTime.Today.AddDays(7);
            var endDate = startDate.AddDays(6);

            // Act
            await _rentalManager.CreateRentalAsync(
                userId,
                booth.Id,
                boothType.Id,
                startDate,
                endDate
            );

            // Assert
            var updatedBooth = await _boothRepository.GetAsync(booth.Id);
            updatedBooth.Status.ShouldBe(BoothStatus.Reserved);
        }

        [Fact]
        public async Task CalculateRentalCostAsync_Should_Calculate_Correct_Cost()
        {
            // Arrange
            var dailyPrice = 150m;
            var boothNumber = $"B{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            var booth = new Booth(Guid.NewGuid(), boothNumber, dailyPrice, DefaultOrganizationalUnitId);
            await _boothRepository.InsertAsync(booth);

            var boothType = new BoothType(Guid.NewGuid(), "Standard", "Standard booth", 10m, DefaultOrganizationalUnitId);
            await _boothTypeRepository.InsertAsync(boothType);

            var startDate = DateTime.Today.AddDays(7);
            var endDate = startDate.AddDays(6); // 7 days
            var period = new RentalPeriod(startDate, endDate);

            // Act
            var cost = await _rentalManager.CalculateRentalCostAsync(booth.Id, boothType.Id, period);

            // Assert
            var expectedCost = dailyPrice * 7;
            cost.ShouldBe(expectedCost);
        }

        [Fact]
        public async Task CalculateRentalCostAsync_Should_Throw_When_Booth_Type_Not_Active()
        {
            // Arrange
            var boothNumber = $"B{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            var booth = new Booth(Guid.NewGuid(), boothNumber, 100m, DefaultOrganizationalUnitId);
            await _boothRepository.InsertAsync(booth);

            var inactiveBoothType = new BoothType(Guid.NewGuid(), "Inactive", "Inactive booth", 10m, DefaultOrganizationalUnitId);
            inactiveBoothType.Deactivate(); // Assuming there's a method to deactivate
            await _boothTypeRepository.InsertAsync(inactiveBoothType);

            var startDate = DateTime.Today.AddDays(7);
            var endDate = startDate.AddDays(6);
            var period = new RentalPeriod(startDate, endDate);

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _rentalManager.CalculateRentalCostAsync(booth.Id, inactiveBoothType.Id, period)
            );

            exception.Code.ShouldBe("BOOTH_TYPE_NOT_AVAILABLE");
        }

        [Fact]
        public async Task ValidateExtensionAsync_Should_Throw_When_New_Rental_Exists_In_Extended_Period()
        {
            // Arrange
            var userId1 = TestUserId1;
            var userId2 = TestUserId2;

            var boothNumber = $"B{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            var booth = new Booth(Guid.NewGuid(), boothNumber, 100m, DefaultOrganizationalUnitId);
            await _boothRepository.InsertAsync(booth);

            var boothType = new BoothType(Guid.NewGuid(), "Standard", "Standard booth", 10m, DefaultOrganizationalUnitId);
            await _boothTypeRepository.InsertAsync(boothType);

            var startDate1 = DateTime.Today.AddDays(7);
            var endDate1 = startDate1.AddDays(6); // Original period

            var rental1 = await _rentalManager.CreateRentalAsync(
                userId1,
                booth.Id,
                boothType.Id,
                startDate1,
                endDate1
            );
            await _rentalRepository.InsertAsync(rental1);

            // Create conflicting rental that starts after original end date
            var startDate2 = endDate1.AddDays(2);
            var endDate2 = startDate2.AddDays(6);

            var rental2 = await _rentalManager.CreateRentalAsync(
                userId2,
                booth.Id,
                boothType.Id,
                startDate2,
                endDate2
            );
            await _rentalRepository.InsertAsync(rental2);

            var newEndDate = endDate2.AddDays(1); // Try to extend to conflict with rental2

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _rentalManager.ValidateExtensionAsync(rental1, newEndDate)
            );

            exception.Code.ShouldBe("CANNOT_EXTEND_DUE_TO_EXISTING_RENTAL");
        }

        [Fact]
        public async Task ValidateGapRulesAsync_Should_Throw_When_Period_Overlaps_Existing_Rental()
        {
            // Arrange
            var userId1 = TestUserId1;
            var userId2 = TestUserId2;

            var boothNumber = $"B{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            var booth = new Booth(Guid.NewGuid(), boothNumber, 100m, DefaultOrganizationalUnitId);
            await _boothRepository.InsertAsync(booth);

            var boothType = new BoothType(Guid.NewGuid(), "Standard", "Standard booth", 10m, DefaultOrganizationalUnitId);
            await _boothTypeRepository.InsertAsync(boothType);

            var startDate1 = DateTime.Today.AddDays(7);
            var endDate1 = startDate1.AddDays(6);

            var rental1 = await _rentalManager.CreateRentalAsync(
                userId1,
                booth.Id,
                boothType.Id,
                startDate1,
                endDate1
            );
            await _rentalRepository.InsertAsync(rental1);

            var overlappingStartDate = startDate1.AddDays(2);
            var overlappingEndDate = overlappingStartDate.AddDays(6);

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _rentalManager.ValidateGapRulesAsync(
                    booth.Id,
                    overlappingStartDate,
                    overlappingEndDate
                )
            );

            exception.Code.ShouldBe("BOOTH_ALREADY_RENTED_IN_PERIOD");
        }

        [Fact]
        public async Task ValidateGapRulesAsync_Should_Exclude_Specific_Rental_When_Checking()
        {
            // Arrange
            var userId = TestUserId1;

            var boothNumber = $"B{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            var booth = new Booth(Guid.NewGuid(), boothNumber, 100m, DefaultOrganizationalUnitId);
            await _boothRepository.InsertAsync(booth);

            var boothType = new BoothType(Guid.NewGuid(), "Standard", "Standard booth", 10m, DefaultOrganizationalUnitId);
            await _boothTypeRepository.InsertAsync(boothType);

            var startDate = DateTime.Today.AddDays(7);
            var endDate = startDate.AddDays(6);

            var rental = await _rentalManager.CreateRentalAsync(
                userId,
                booth.Id,
                boothType.Id,
                startDate,
                endDate
            );
            await _rentalRepository.InsertAsync(rental);

            // Act - should not throw because we exclude this rental
            await _rentalManager.ValidateGapRulesAsync(
                booth.Id,
                startDate,
                endDate,
                rental.Id
            );

            // Assert - if we get here without exception, test passes
        }
    }
}
