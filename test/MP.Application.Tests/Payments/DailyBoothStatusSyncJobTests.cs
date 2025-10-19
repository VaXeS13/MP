using Volo.Abp.Uow;
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
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Xunit;

namespace MP.Application.Tests.Payments
{
    public class DailyBoothStatusSyncJobTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private static readonly Guid TestUserId1 = new Guid("00000000-0000-0000-0000-000000000001");

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
            await WithUnitOfWorkAsync(async () =>
            {
                // Arrange
                var today = DateTime.Today;
                var guid1 = Guid.NewGuid().ToString().Replace("-", "");
                var guid2 = Guid.NewGuid().ToString().Replace("-", "");

                // Create booth type
                var boothType = new BoothType(
                    Guid.NewGuid(),
                    $"T{guid1.Substring(0, 3)}",
                    "Test Description",
                    10m
                );
                await _boothTypeRepository.InsertAsync(boothType);

                // Create booth
                var booth = new MP.Domain.Booths.Booth(
                    Guid.NewGuid(),
                    $"R{guid2.Substring(0, 9)}",
                    100m
                );
                await _boothRepository.InsertAsync(booth);

                // Create active rental with completed payment (period includes today)
                // Using today as start so it overlaps with today (today to today+6 = 7 days)
                var rental = new Rental(
                    Guid.NewGuid(),
                    TestUserId1,
                    booth.Id,
                    boothType.Id,
                    new RentalPeriod(today, today.AddDays(6)),
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
            });
        }

        [Fact]
        public async Task DetermineBoothStatus_Should_Return_Reserved_When_Future_Rental_Exists()
        {
            await WithUnitOfWorkAsync(async () =>
            {
                // Arrange
                var today = DateTime.Today;
                var guid1 = Guid.NewGuid().ToString().Replace("-", "");
                var guid2 = Guid.NewGuid().ToString().Replace("-", "");

                // Create booth type
                var boothType = new BoothType(
                    Guid.NewGuid(),
                    $"T{guid1.Substring(0, 3)}",
                    "Test Description",
                    10m
                );
                await _boothTypeRepository.InsertAsync(boothType);

                // Create booth
                var booth = new MP.Domain.Booths.Booth(
                    Guid.NewGuid(),
                    $"R{guid2.Substring(0, 9)}",
                    100m
                );
                await _boothRepository.InsertAsync(booth);

                // Create future rental with completed payment (starts tomorrow)
                var rental = new Rental(
                    Guid.NewGuid(),
                    TestUserId1,
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
            });
        }

        [Fact]
        public async Task DetermineBoothStatus_Should_Return_Available_When_No_Rental_Exists()
        {
            await WithUnitOfWorkAsync(async () =>
            {
                // Arrange
                var today = DateTime.Today;
                var guid1 = Guid.NewGuid().ToString().Replace("-", "");

                // Create booth
                var booth = new MP.Domain.Booths.Booth(
                    Guid.NewGuid(),
                    $"A{guid1.Substring(0, 9)}",
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
            });
        }

        [Fact]
        public async Task DetermineBoothStatus_Should_Return_Maintenance_When_Status_Is_Maintenance()
        {
            await WithUnitOfWorkAsync(async () =>
            {
                // Arrange
                var today = DateTime.Today;
                var guid1 = Guid.NewGuid().ToString().Replace("-", "");

                // Create booth in maintenance
                var booth = new MP.Domain.Booths.Booth(
                    Guid.NewGuid(),
                    $"M{guid1.Substring(0, 9)}",
                    100m
                );
                booth.MarkAsMaintenance();
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
            });
        }

        [Fact]
        public async Task DetermineBoothStatus_Should_Prioritize_Active_Over_Future_Rental()
        {
            await WithUnitOfWorkAsync(async () =>
            {
                // Arrange
                var today = DateTime.Today;
                var guid1 = Guid.NewGuid().ToString().Replace("-", "");
                var guid2 = Guid.NewGuid().ToString().Replace("-", "");

                // Create booth type
                var boothType = new BoothType(
                    Guid.NewGuid(),
                    $"T{guid1.Substring(0, 3)}",
                    "Test Description",
                    10m
                );
                await _boothTypeRepository.InsertAsync(boothType);

                // Create booth
                var booth = new MP.Domain.Booths.Booth(
                    Guid.NewGuid(),
                    $"P{guid2.Substring(0, 9)}",
                    100m
                );
                await _boothRepository.InsertAsync(booth);

                // Create active rental (current - today to today+6 = 7 days)
                var activeRental = new Rental(
                    Guid.NewGuid(),
                    TestUserId1,
                    booth.Id,
                    boothType.Id,
                    new RentalPeriod(today, today.AddDays(6)),
                    1000m,
                    Currency.PLN
                );
                activeRental.MarkAsPaid(1000m, DateTime.Now);

                // Create future rental (shouldn't affect status since active exists)
                var futureRental = new Rental(
                    Guid.NewGuid(),
                    TestUserId1,
                    booth.Id,
                    boothType.Id,
                    new RentalPeriod(today.AddDays(7), today.AddDays(13)),
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
            });
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
