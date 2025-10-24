using Volo.Abp.Uow;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MP.Application.Payments;
using MP.Domain.Booths;
using MP.Domain.BoothTypes;
using MP.Domain.Payments;
using MP.Domain.Rentals;
using MP.Rentals;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Xunit;

namespace MP.Application.Tests.Payments
{
    public class P24StatusCheckRecurringJobTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private static readonly Guid DefaultOrganizationalUnitId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private static readonly Guid TestUserId1 = new Guid("00000000-0000-0000-0000-000000000001");
        private readonly IBoothRepository _boothRepository;
        private readonly IRepository<Rental, Guid> _rentalRepository;
        private readonly IRepository<BoothType, Guid> _boothTypeRepository;
        private readonly IRepository<IdentityUser, Guid> _userRepository;
        private readonly IP24TransactionRepository _p24TransactionRepository;

        public P24StatusCheckRecurringJobTests()
        {
            _boothRepository = GetRequiredService<IBoothRepository>();
            _rentalRepository = GetRequiredService<IRepository<Rental, Guid>>();
            _boothTypeRepository = GetRequiredService<IRepository<BoothType, Guid>>();
            _userRepository = GetRequiredService<IRepository<IdentityUser, Guid>>();
            _p24TransactionRepository = GetRequiredService<IP24TransactionRepository>();
        }

        [Fact]
        [UnitOfWork]
        public async Task Should_Update_Rental_And_Booth_When_Payment_Verified()
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
                10m,
                DefaultOrganizationalUnitId
            );
            await _boothTypeRepository.InsertAsync(boothType);

            // Create booth (initially Reserved)
            var booth = new MP.Domain.Booths.Booth(
                Guid.NewGuid(),
                $"P{guid2.Substring(0, 9)}",
                100m,
                DefaultOrganizationalUnitId
            );
            booth.MarkAsReserved();
            await _boothRepository.InsertAsync(booth);

            // Create rental (Draft status, not paid yet)
            var sessionId = $"p24_test_{Guid.NewGuid()}";
            var rental = new Rental(
                Guid.NewGuid(),
                TestUserId1,
                booth.Id,
                boothType.Id,
                DefaultOrganizationalUnitId,
                new RentalPeriod(today, today.AddDays(10)),
                1000m,
                Currency.PLN
            );
            rental.Payment.SetTransactionId(sessionId);
            await _rentalRepository.InsertAsync(rental);

            // Create P24 transaction (not yet verified)
            var transaction = new P24Transaction(
                Guid.NewGuid(),
                sessionId,
                142798, // merchantId
                142798, // posId
                1000m,
                "PLN",
                "test@test.com",
                "Test payment",
                "test-sign"
            );
            await _p24TransactionRepository.InsertAsync(transaction);

            // Act - Simulate payment verification
            transaction.SetStatus("completed");
            transaction.SetVerified(true);
            await _p24TransactionRepository.UpdateAsync(transaction);

            // Simulate what UpdateRentalsAndBoothsAfterPaymentAsync does:
            var rentalToUpdate = await _rentalRepository.FirstOrDefaultAsync(r =>
                r.Payment.Przelewy24TransactionId == sessionId);
            rentalToUpdate.ShouldNotBeNull();

            if (!rentalToUpdate.Payment.IsPaid)
            {
                rentalToUpdate.MarkAsPaid(rentalToUpdate.Payment.TotalAmount, DateTime.Now, sessionId);

                var boothToUpdate = await _boothRepository.GetAsync(rentalToUpdate.BoothId);
                if (boothToUpdate.Status != BoothStatus.Maintenance)
                {
                    if (rentalToUpdate.Period.StartDate <= DateTime.Today)
                    {
                        boothToUpdate.MarkAsRented();
                    }
                    else
                    {
                        boothToUpdate.MarkAsReserved();
                    }
                    await _boothRepository.UpdateAsync(boothToUpdate);
                }
                await _rentalRepository.UpdateAsync(rentalToUpdate);
            }

            // Assert
            var updatedRental = await _rentalRepository.GetAsync(rental.Id);
            updatedRental.Payment.IsPaid.ShouldBeTrue();
            updatedRental.Status.ShouldBe(RentalStatus.Active);

            var updatedBooth = await _boothRepository.GetAsync(booth.Id);
            updatedBooth.Status.ShouldBe(BoothStatus.Rented); // Since StartDate is today
        }

        [Fact]
        [UnitOfWork]
        public async Task Should_Not_Update_Booth_Status_If_In_Maintenance()
        {
            // Arrange
            var today = DateTime.Today;

            // Create booth type
            var boothType = new BoothType(
                Guid.NewGuid(),
                $"T{Guid.NewGuid().ToString().Replace("-", "").Substring(0, 3)}",
                "Test Description",
                10m,
                DefaultOrganizationalUnitId
            );
            await _boothTypeRepository.InsertAsync(boothType);

            // Create booth in Maintenance
            var booth = new MP.Domain.Booths.Booth(
                Guid.NewGuid(),
                $"M{Guid.NewGuid().ToString().Replace("-", "").Substring(0, 9)}",
                100m,
                DefaultOrganizationalUnitId
            );
            booth.MarkAsMaintenance();
            await _boothRepository.InsertAsync(booth);

            // Create rental
            var sessionId = $"p24_maint_test_{Guid.NewGuid()}";
            var rental = new Rental(
                Guid.NewGuid(),
                TestUserId1,
                booth.Id,
                boothType.Id,
                DefaultOrganizationalUnitId,
                new RentalPeriod(today, today.AddDays(10)),
                1000m,
                Currency.PLN
            );
            rental.Payment.SetTransactionId(sessionId);
            await _rentalRepository.InsertAsync(rental);

            // Act - This is what the job does when payment is verified
            // It marks rental as paid, then tries to update booth status
            rental.MarkAsPaid(rental.Payment.TotalAmount, DateTime.Now, sessionId);

            var boothToUpdate = await _boothRepository.GetAsync(rental.BoothId);
            // The job's logic: only update booth status if NOT in Maintenance
            if (boothToUpdate.Status != BoothStatus.Maintenance)
            {
                if (rental.Period.StartDate <= DateTime.Today)
                {
                    boothToUpdate.MarkAsRented();
                }
                else
                {
                    boothToUpdate.MarkAsReserved();
                }
                await _boothRepository.UpdateAsync(boothToUpdate);
            }

            await _rentalRepository.UpdateAsync(rental);

            // Assert - booth should still be in Maintenance
            var updatedBooth = await _boothRepository.GetAsync(booth.Id);
            updatedBooth.Status.ShouldBe(BoothStatus.Maintenance);
        }

        [Fact]
        [UnitOfWork]
        public async Task Should_Mark_Booth_As_Reserved_When_Rental_Starts_In_Future()
        {
            // Arrange
            var today = DateTime.Today;

            // Create booth type
            var boothType = new BoothType(
                Guid.NewGuid(),
                $"T{Guid.NewGuid().ToString().Replace("-", "").Substring(0, 3)}",
                "Test Description",
                10m,
                DefaultOrganizationalUnitId
            );
            await _boothTypeRepository.InsertAsync(boothType);

            // Create booth
            var booth = new MP.Domain.Booths.Booth(
                Guid.NewGuid(),
                $"F{Guid.NewGuid().ToString().Replace("-", "").Substring(0, 9)}",
                100m,
                DefaultOrganizationalUnitId
            );
            await _boothRepository.InsertAsync(booth);

            // Create rental that starts tomorrow
            var sessionId = $"p24_future_test_{Guid.NewGuid()}";
            var rental = new Rental(
                Guid.NewGuid(),
                TestUserId1,
                booth.Id,
                boothType.Id,
                DefaultOrganizationalUnitId,
                new RentalPeriod(today.AddDays(1), today.AddDays(10)),
                1000m,
                Currency.PLN
            );
            rental.Payment.SetTransactionId(sessionId);
            await _rentalRepository.InsertAsync(rental);

            // Act - Mark rental as paid
            rental.MarkAsPaid(rental.Payment.TotalAmount, DateTime.Now, sessionId);

            var boothToUpdate = await _boothRepository.GetAsync(rental.BoothId);
            if (boothToUpdate.Status != BoothStatus.Maintenance)
            {
                // Since rental starts tomorrow, mark as Reserved (not Rented)
                if (rental.Period.StartDate <= DateTime.Today)
                {
                    boothToUpdate.MarkAsRented();
                }
                else
                {
                    boothToUpdate.MarkAsReserved();
                }
                await _boothRepository.UpdateAsync(boothToUpdate);
            }

            await _rentalRepository.UpdateAsync(rental);

            // Assert
            var updatedBooth = await _boothRepository.GetAsync(booth.Id);
            updatedBooth.Status.ShouldBe(BoothStatus.Reserved); // Not Rented yet
        }

        [Fact]
        [UnitOfWork]
        public async Task Should_Cancel_Rental_When_Max_Status_Checks_Reached()
        {
            // Arrange
            var today = DateTime.Today;

            // Create booth type
            var boothType = new BoothType(
                Guid.NewGuid(),
                $"T{Guid.NewGuid().ToString().Replace("-", "").Substring(0, 3)}",
                "Test Description",
                10m,
                DefaultOrganizationalUnitId
            );
            await _boothTypeRepository.InsertAsync(boothType);

            // Create booth
            var booth = new MP.Domain.Booths.Booth(
                Guid.NewGuid(),
                $"C{Guid.NewGuid().ToString().Replace("-", "").Substring(0, 9)}",
                100m,
                DefaultOrganizationalUnitId
            );
            booth.MarkAsReserved();
            await _boothRepository.InsertAsync(booth);

            // Create rental (not paid)
            var sessionId = $"p24_cancel_test_{Guid.NewGuid()}";
            var rental = new Rental(
                Guid.NewGuid(),
                TestUserId1,
                booth.Id,
                boothType.Id,
                DefaultOrganizationalUnitId,
                new RentalPeriod(today, today.AddDays(10)),
                1000m,
                Currency.PLN
            );
            rental.Payment.SetTransactionId(sessionId);
            await _rentalRepository.InsertAsync(rental);

            // Create P24 transaction (failed after 3 checks)
            var transaction = new P24Transaction(
                Guid.NewGuid(),
                sessionId,
                142798,
                142798,
                1000m,
                "PLN",
                "test@test.com",
                "Test payment",
                "test-sign"
            );
            transaction.IncrementStatusCheckCount();
            transaction.IncrementStatusCheckCount();
            transaction.IncrementStatusCheckCount(); // 3 checks reached
            await _p24TransactionRepository.InsertAsync(transaction);

            // Act - Simulate HandleMaxStatusChecksReached logic
            if (transaction.ManualStatusCheckCount >= 3 && transaction.Status != "completed")
            {
                var rentalToCancel = await _rentalRepository.FirstOrDefaultAsync(r =>
                    r.Payment.Przelewy24TransactionId == transaction.SessionId);

                if (rentalToCancel != null)
                {
                    var boothToRelease = await _boothRepository.GetAsync(rentalToCancel.BoothId);

                    rentalToCancel.Cancel("Payment not completed within allowed time");
                    boothToRelease.MarkAsAvailable();

                    await _rentalRepository.UpdateAsync(rentalToCancel);
                    await _boothRepository.UpdateAsync(boothToRelease);
                }
            }

            // Assert
            var updatedRental = await _rentalRepository.GetAsync(rental.Id);
            updatedRental.Status.ShouldBe(RentalStatus.Cancelled);

            var updatedBooth = await _boothRepository.GetAsync(booth.Id);
            updatedBooth.Status.ShouldBe(BoothStatus.Available);
        }
    }
}
