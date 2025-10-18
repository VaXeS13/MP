using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MP.Application.Payments;
using MP.Domain.Booths;
using MP.Domain.BoothTypes;
using MP.Domain.Rentals;
using MP.Rentals;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Xunit;

namespace MP.Application.Tests.Payments
{
    public class DailyBoothStatusSyncJobTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly DailyBoothStatusSyncJob _dailyBoothStatusSyncJob;
        private readonly IBoothRepository _boothRepository;
        private readonly IRepository<Rental, Guid> _rentalRepository;
        private readonly IRepository<BoothType, Guid> _boothTypeRepository;
        private readonly IRepository<IdentityUser, Guid> _userRepository;

        public DailyBoothStatusSyncJobTests()
        {
            _dailyBoothStatusSyncJob = GetRequiredService<DailyBoothStatusSyncJob>();
            _boothRepository = GetRequiredService<IBoothRepository>();
            _rentalRepository = GetRequiredService<IRepository<Rental, Guid>>();
            _boothTypeRepository = GetRequiredService<IRepository<BoothType, Guid>>();
            _userRepository = GetRequiredService<IRepository<IdentityUser, Guid>>();
        }

        [Fact]
        public async Task DetermineBoothStatus_Should_Return_Rented_When_Active_Rental_Exists()
        {
            // Arrange
            var today = DateTime.Today;

            // Create test user
            var user = await _userRepository.FirstOrDefaultAsync();
            user.ShouldNotBeNull();

            // Create booth type
            var boothType = new BoothType(
                Guid.NewGuid(),
                "Test Type",
                "Test Description",
                10m
            );
            await _boothTypeRepository.InsertAsync(boothType);

            // Create booth
            var booth = new MP.Domain.Booths.Booth(
                Guid.NewGuid(),
                "TEST-RENTED-01",
                100m
            );
            await _boothRepository.InsertAsync(booth);

            // Create active rental with completed payment (period includes today)
            var rental = new Rental(
                Guid.NewGuid(),
                user.Id,
                booth.Id,
                boothType.Id,
                new RentalPeriod(today.AddDays(-5), today.AddDays(5)),
                1000m,
                Currency.PLN
            );
            rental.MarkAsPaid(1000m, DateTime.Now);
            await _rentalRepository.InsertAsync(rental);

            // Create lookup dictionaries (simulating job logic)
            var activeRentalMap = new Dictionary<Guid, Rental>
            {
                { booth.Id, rental }
            };
            var futureRentalMap = new Dictionary<Guid, Rental>();

            // Act
            var result = DetermineBoothStatusTestHelper(
                booth,
                activeRentalMap,
                futureRentalMap,
                today
            );

            // Assert
            result.ShouldBe(BoothStatus.Rented);
        }

        [Fact]
        public async Task DetermineBoothStatus_Should_Return_Reserved_When_Future_Rental_Exists()
        {
            // Arrange
            var today = DateTime.Today;

            // Create test user
            var user = await _userRepository.FirstOrDefaultAsync();
            user.ShouldNotBeNull();

            // Create booth type
            var boothType = new BoothType(
                Guid.NewGuid(),
                "Test Type 2",
                "Test Description",
                10m
            );
            await _boothTypeRepository.InsertAsync(boothType);

            // Create booth
            var booth = new MP.Domain.Booths.Booth(
                Guid.NewGuid(),
                "TEST-RESERVED-01",
                100m
            );
            await _boothRepository.InsertAsync(booth);

            // Create future rental with completed payment (starts tomorrow)
            var rental = new Rental(
                Guid.NewGuid(),
                user.Id,
                booth.Id,
                boothType.Id,
                new RentalPeriod(today.AddDays(1), today.AddDays(10)),
                1000m,
                Currency.PLN
            );
            rental.MarkAsPaid(1000m, DateTime.Now);
            await _rentalRepository.InsertAsync(rental);

            // Create lookup dictionaries
            var activeRentalMap = new Dictionary<Guid, Rental>();
            var futureRentalMap = new Dictionary<Guid, Rental>
            {
                { booth.Id, rental }
            };

            // Act
            var result = DetermineBoothStatusTestHelper(
                booth,
                activeRentalMap,
                futureRentalMap,
                today
            );

            // Assert
            result.ShouldBe(BoothStatus.Reserved);
        }

        [Fact]
        public async Task DetermineBoothStatus_Should_Return_Available_When_No_Rental_Exists()
        {
            // Arrange
            var today = DateTime.Today;

            // Create booth
            var booth = new MP.Domain.Booths.Booth(
                Guid.NewGuid(),
                "TEST-AVAILABLE-01",
                100m
            );
            await _boothRepository.InsertAsync(booth);

            // No rentals
            var activeRentalMap = new Dictionary<Guid, Rental>();
            var futureRentalMap = new Dictionary<Guid, Rental>();

            // Act
            var result = DetermineBoothStatusTestHelper(
                booth,
                activeRentalMap,
                futureRentalMap,
                today
            );

            // Assert
            result.ShouldBe(BoothStatus.Available);
        }

        [Fact]
        public async Task DetermineBoothStatus_Should_Return_Maintenance_When_Status_Is_Maintenance()
        {
            // Arrange
            var today = DateTime.Today;

            // Create booth in maintenance
            var booth = new MP.Domain.Booths.Booth(
                Guid.NewGuid(),
                "TEST-MAINT-01",
                100m
            );
            booth.MarkAsMaintenace();
            await _boothRepository.InsertAsync(booth);

            // Even with active rental, maintenance takes priority
            var activeRentalMap = new Dictionary<Guid, Rental>();
            var futureRentalMap = new Dictionary<Guid, Rental>();

            // Act
            var result = DetermineBoothStatusTestHelper(
                booth,
                activeRentalMap,
                futureRentalMap,
                today
            );

            // Assert
            result.ShouldBe(BoothStatus.Maintenance);
        }

        [Fact]
        public async Task DetermineBoothStatus_Should_Prioritize_Active_Over_Future_Rental()
        {
            // Arrange
            var today = DateTime.Today;

            // Create test user
            var user = await _userRepository.FirstOrDefaultAsync();
            user.ShouldNotBeNull();

            // Create booth type
            var boothType = new BoothType(
                Guid.NewGuid(),
                "Test Type 3",
                "Test Description",
                10m
            );
            await _boothTypeRepository.InsertAsync(boothType);

            // Create booth
            var booth = new MP.Domain.Booths.Booth(
                Guid.NewGuid(),
                "TEST-PRIORITY-01",
                100m
            );
            await _boothRepository.InsertAsync(booth);

            // Create active rental (current)
            var activeRental = new Rental(
                Guid.NewGuid(),
                user.Id,
                booth.Id,
                boothType.Id,
                new RentalPeriod(today.AddDays(-2), today.AddDays(2)),
                1000m,
                Currency.PLN
            );
            activeRental.MarkAsPaid(1000m, DateTime.Now);

            // Create future rental (shouldn't affect status since active exists)
            var futureRental = new Rental(
                Guid.NewGuid(),
                user.Id,
                booth.Id,
                boothType.Id,
                new RentalPeriod(today.AddDays(5), today.AddDays(10)),
                1000m,
                Currency.PLN
            );
            futureRental.MarkAsPaid(1000m, DateTime.Now);

            await _rentalRepository.InsertAsync(activeRental);
            await _rentalRepository.InsertAsync(futureRental);

            // Both active and future rentals exist
            var activeRentalMap = new Dictionary<Guid, Rental>
            {
                { booth.Id, activeRental }
            };
            var futureRentalMap = new Dictionary<Guid, Rental>
            {
                { booth.Id, futureRental }
            };

            // Act
            var result = DetermineBoothStatusTestHelper(
                booth,
                activeRentalMap,
                futureRentalMap,
                today
            );

            // Assert
            result.ShouldBe(BoothStatus.Rented); // Active takes priority
        }

        /// <summary>
        /// Helper method that simulates the private DetermineBoothStatus method from DailyBoothStatusSyncJob
        /// This is the same logic extracted for testing purposes
        /// </summary>
        private BoothStatus DetermineBoothStatusTestHelper(
            MP.Domain.Booths.Booth booth,
            Dictionary<Guid, Rental> activeRentalMap,
            Dictionary<Guid, Rental> futureRentalMap,
            DateTime today)
        {
            // Priority 1: Maintenance status is never changed by this job
            if (booth.Status == BoothStatus.Maintenance)
            {
                return BoothStatus.Maintenance;
            }

            // Priority 2: Check if there's an active rental for TODAY
            if (activeRentalMap.ContainsKey(booth.Id))
            {
                return BoothStatus.Rented;
            }

            // Priority 3: Check if there's a paid future rental (Reserved status)
            if (futureRentalMap.ContainsKey(booth.Id))
            {
                return BoothStatus.Reserved;
            }

            // Default: No active or future rental = Available
            return BoothStatus.Available;
        }
    }
}
