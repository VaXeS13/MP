using Volo.Abp.Uow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MP.Rentals;
using MP.Domain.Booths;
using MP.Domain.BoothTypes;
using MP.Domain.Rentals;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Xunit;

namespace MP.Application.Tests.Rentals
{
    public class RentalAppServiceTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private static readonly Guid TestUserId1 = new Guid("00000000-0000-0000-0000-000000000001");
        private static readonly Guid TestOrganizationalUnitId = new Guid("00000000-0000-0000-0000-000000000010");

        private readonly IRentalAppService _rentalAppService;
        private readonly IBoothRepository _boothRepository;
        private readonly IBoothTypeRepository _boothTypeRepository;
        private readonly IRepository<Rental, Guid> _rentalRepository;
        private readonly IRepository<IdentityUser, Guid> _userRepository;

        public RentalAppServiceTests()
        {
            _rentalAppService = GetRequiredService<IRentalAppService>();
            _boothRepository = GetRequiredService<IBoothRepository>();
            _boothTypeRepository = GetRequiredService<IBoothTypeRepository>();
            _rentalRepository = GetRequiredService<IRepository<Rental, Guid>>();
            _userRepository = GetRequiredService<IRepository<IdentityUser, Guid>>();
        }

        private async Task CleanupRentalsForTestUserAsync()
        {
            // Clean up rentals for the test user to avoid accumulation between tests
            var rentals = await _rentalRepository.GetListAsync(r => r.UserId == TestUserId1);
            foreach (var rental in rentals)
            {
                await _rentalRepository.DeleteAsync(rental);
            }
        }

        [Fact]
        [UnitOfWork]
        public async Task CreateMyRentalAsync_Should_Create_Rental_For_Current_User()
        {
            // Arrange
            var booth = await CreateTestBoothAsync();
            var boothType = await CreateTestBoothTypeAsync();

            var createDto = new CreateMyRentalDto
            {
                BoothId = booth.Id,
                BoothTypeId = boothType.Id,
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(13),
                Notes = "Test rental"
            };

            // Act
            var result = await _rentalAppService.CreateMyRentalAsync(createDto);

            // Assert
            result.ShouldNotBeNull();
            result.BoothId.ShouldBe(booth.Id);
            result.Status.ShouldBe(RentalStatus.Draft);
            result.DaysCount.ShouldBe(7);
        }

        [Fact]
        [UnitOfWork]
        public async Task CreateMyRentalAsync_Should_Calculate_Total_Amount()
        {
            // Arrange
            var boothPrice = 100m;
            var booth = await CreateTestBoothAsync(boothPrice);
            var boothType = await CreateTestBoothTypeAsync();

            var createDto = new CreateMyRentalDto
            {
                BoothId = booth.Id,
                BoothTypeId = boothType.Id,
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(13), // 7 days
                Notes = null
            };

            // Act
            var result = await _rentalAppService.CreateMyRentalAsync(createDto);

            // Assert
            var expectedAmount = boothPrice * 7;
            result.TotalAmount.ShouldBe(expectedAmount);
        }

        [Fact]
        [UnitOfWork]
        public async Task CreateMyRentalAsync_Should_Throw_When_Booth_In_Maintenance()
        {
            // Arrange
            var booth = await CreateTestBoothAsync();
            booth.MarkAsMaintenance();
            await _boothRepository.UpdateAsync(booth);

            var boothType = await CreateTestBoothTypeAsync();

            var createDto = new CreateMyRentalDto
            {
                BoothId = booth.Id,
                BoothTypeId = boothType.Id,
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(13)
            };

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _rentalAppService.CreateMyRentalAsync(createDto)
            );

            exception.Code.ShouldBe("BOOTH_IN_MAINTENANCE");
        }

        [Fact]
        [UnitOfWork]
        public async Task GetMyRentalsAsync_Should_Return_Only_Current_User_Rentals()
        {
            // Arrange - cleanup previous rentals for this user
            await CleanupRentalsForTestUserAsync();

            var booth1 = await CreateTestBoothAsync();
            var booth2 = await CreateTestBoothAsync($"BOOTH{Guid.NewGuid().ToString().Substring(0, 5)}".ToUpper());
            var boothType = await CreateTestBoothTypeAsync();

            var createDto1 = new CreateMyRentalDto
            {
                BoothId = booth1.Id,
                BoothTypeId = boothType.Id,
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(13)
            };

            var createDto2 = new CreateMyRentalDto
            {
                BoothId = booth2.Id,
                BoothTypeId = boothType.Id,
                StartDate = DateTime.Today.AddDays(20),
                EndDate = DateTime.Today.AddDays(26)
            };

            await _rentalAppService.CreateMyRentalAsync(createDto1);
            await _rentalAppService.CreateMyRentalAsync(createDto2);

            // Act
            var result = await _rentalAppService.GetMyRentalsAsync(new GetRentalListDto());

            // Assert
            result.Items.ShouldNotBeEmpty();
            result.TotalCount.ShouldBe(2);
            result.Items.ToList().ForEach(r => r.UserId.ShouldBe(TestUserId1));
        }

        [Fact]
        [UnitOfWork]
        public async Task CheckAvailabilityAsync_Should_Return_True_For_Available_Booth()
        {
            // Arrange
            var booth = await CreateTestBoothAsync();
            var startDate = DateTime.Today.AddDays(7);
            var endDate = DateTime.Today.AddDays(13);

            // Act
            var result = await _rentalAppService.CheckAvailabilityAsync(booth.Id, startDate, endDate);

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        [UnitOfWork]
        public async Task CheckAvailabilityAsync_Should_Return_False_When_Booth_Already_Rented()
        {
            // Arrange
            var booth = await CreateTestBoothAsync();
            var boothType = await CreateTestBoothTypeAsync();

            // Create existing rental
            var createDto = new CreateMyRentalDto
            {
                BoothId = booth.Id,
                BoothTypeId = boothType.Id,
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(13)
            };

            var rental = await _rentalAppService.CreateMyRentalAsync(createDto);

            // Mark as active
            var domainRental = await _rentalRepository.GetAsync(rental.Id);
            domainRental.MarkAsPaid(rental.TotalAmount, DateTime.Now);
            await _rentalRepository.UpdateAsync(domainRental);

            // Act - Check same period
            var result = await _rentalAppService.CheckAvailabilityAsync(
                booth.Id,
                DateTime.Today.AddDays(8),
                DateTime.Today.AddDays(12)
            );

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        [UnitOfWork]
        public async Task CalculateCostAsync_Should_Return_Correct_Amount()
        {
            // Arrange
            var boothPrice = 150m;
            var booth = await CreateTestBoothAsync(boothPrice);
            var boothType = await CreateTestBoothTypeAsync();

            var startDate = DateTime.Today.AddDays(7);
            var endDate = DateTime.Today.AddDays(16); // 10 days

            // Act
            var result = await _rentalAppService.CalculateCostAsync(
                booth.Id,
                boothType.Id,
                startDate,
                endDate
            );

            // Assert
            var expectedCost = boothPrice * 10;
            result.ShouldBe(expectedCost);
        }

        [Fact]
        [UnitOfWork]
        public async Task GetAsync_Should_Return_Rental_Details()
        {
            // Arrange
            var booth = await CreateTestBoothAsync();
            var boothType = await CreateTestBoothTypeAsync();

            var createDto = new CreateMyRentalDto
            {
                BoothId = booth.Id,
                BoothTypeId = boothType.Id,
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(13)
            };

            var created = await _rentalAppService.CreateMyRentalAsync(createDto);

            // Act
            var result = await _rentalAppService.GetAsync(created.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(created.Id);
            result.BoothId.ShouldBe(booth.Id);
            result.Status.ShouldBe(RentalStatus.Draft);
        }

        [Fact]
        [UnitOfWork]
        public async Task GetListAsync_Should_Return_All_Rentals_With_Pagination()
        {
            // Arrange
            var booth = await CreateTestBoothAsync();
            var boothType = await CreateTestBoothTypeAsync();

            // Create multiple rentals
            for (int i = 0; i < 3; i++)
            {
                var createDto = new CreateMyRentalDto
                {
                    BoothId = booth.Id,
                    BoothTypeId = boothType.Id,
                    StartDate = DateTime.Today.AddDays(7 + (i * 10)),
                    EndDate = DateTime.Today.AddDays(13 + (i * 10))
                };

                await _rentalAppService.CreateMyRentalAsync(createDto);
            }

            // Act
            var result = await _rentalAppService.GetListAsync(new GetRentalListDto
            {
                MaxResultCount = 10,
                SkipCount = 0
            });

            // Assert
            result.Items.ShouldNotBeEmpty();
            result.TotalCount.ShouldBeGreaterThanOrEqualTo(3);
        }

        [Fact]
        [UnitOfWork]
        public async Task CancelRentalAsync_Should_Cancel_Draft_Rental()
        {
            // Arrange
            var booth = await CreateTestBoothAsync();
            var boothType = await CreateTestBoothTypeAsync();

            var createDto = new CreateMyRentalDto
            {
                BoothId = booth.Id,
                BoothTypeId = boothType.Id,
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(13)
            };

            var rental = await _rentalAppService.CreateMyRentalAsync(createDto);

            // Act
            var result = await _rentalAppService.CancelRentalAsync(rental.Id, "User requested cancellation");

            // Assert
            result.Status.ShouldBe(RentalStatus.Cancelled);
        }

        [Fact]
        [UnitOfWork]
        public async Task GetActiveRentalsAsync_Should_Return_Only_Active_Rentals()
        {
            // Arrange
            var booth = await CreateTestBoothAsync();
            var boothType = await CreateTestBoothTypeAsync();

            // Create active rental
            var createDtoActive = new CreateMyRentalDto
            {
                BoothId = booth.Id,
                BoothTypeId = boothType.Id,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(8)
            };

            var activeRental = await _rentalAppService.CreateMyRentalAsync(createDtoActive);
            var domainRental = await _rentalRepository.GetAsync(activeRental.Id);
            domainRental.MarkAsPaid(activeRental.TotalAmount, DateTime.Now);
            await _rentalRepository.UpdateAsync(domainRental);

            // Create draft rental
            var createDtoDraft = new CreateMyRentalDto
            {
                BoothId = booth.Id,
                BoothTypeId = boothType.Id,
                StartDate = DateTime.Today.AddDays(20),
                EndDate = DateTime.Today.AddDays(26)
            };

            await _rentalAppService.CreateMyRentalAsync(createDtoDraft);

            // Act
            var result = await _rentalAppService.GetActiveRentalsAsync();

            // Assert
            result.ShouldNotBeEmpty();
            result.ToList().ForEach(r =>
            {
                r.Status.ShouldBe(RentalStatus.Active);
            });
        }

        // Helper methods
        private async Task<MP.Domain.Booths.Booth> CreateTestBoothAsync(decimal price = 100m)
        {
            return await CreateTestBoothAsync($"BT{Guid.NewGuid().ToString().Substring(0, 6)}", price);
        }

        private async Task<MP.Domain.Booths.Booth> CreateTestBoothAsync(string number, decimal price = 100m)
        {
            var booth = new MP.Domain.Booths.Booth(Guid.NewGuid(), number, price, TestOrganizationalUnitId);
            await _boothRepository.InsertAsync(booth);
            return booth;
        }

        private async Task<BoothType> CreateTestBoothTypeAsync()
        {
            var boothType = new BoothType(
                Guid.NewGuid(),
                $"BoothType-{Guid.NewGuid().ToString().Substring(0, 8)}",
                "Test booth type",
                10m,
                TestOrganizationalUnitId
            );
            await _boothTypeRepository.InsertAsync(boothType);
            return boothType;
        }
    }
}
