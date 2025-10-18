using System;
using MP.Domain.Rentals;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace MP.Domain.Tests.Rentals
{
    public class RentalPeriodTests : MPDomainTestBase<MPDomainTestModule>
    {
        [Fact]
        public void Constructor_Should_Create_Valid_Period()
        {
            // Arrange
            var startDate = DateTime.Today;
            var endDate = DateTime.Today.AddDays(6);

            // Act
            var period = new RentalPeriod(startDate, endDate);

            // Assert
            period.StartDate.ShouldBe(startDate);
            period.EndDate.ShouldBe(endDate);
        }

        [Fact]
        public void Constructor_Should_Throw_When_EndDate_Before_StartDate()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(5);
            var endDate = DateTime.Today;

            // Act & Assert
            var exception = Should.Throw<BusinessException>(
                () => new RentalPeriod(startDate, endDate)
            );

            exception.Code.ShouldBe("RENTAL_PERIOD_INVALID_DATES");
        }

        [Fact]
        public void Constructor_Should_Throw_When_EndDate_Equals_StartDate()
        {
            // Arrange
            var startDate = DateTime.Today;
            var endDate = DateTime.Today;

            // Act & Assert
            var exception = Should.Throw<BusinessException>(
                () => new RentalPeriod(startDate, endDate)
            );

            exception.Code.ShouldBe("RENTAL_PERIOD_INVALID_DATES");
        }

        [Fact]
        public void DaysCount_Should_Calculate_Correctly()
        {
            // Arrange
            var startDate = DateTime.Today;
            var endDate = DateTime.Today.AddDays(6); // 7 days total

            var period = new RentalPeriod(startDate, endDate);

            // Act
            var daysCount = period.DaysCount;

            // Assert
            daysCount.ShouldBe(7);
        }

        [Fact]
        public void DaysCount_Should_Include_Both_Start_And_End_Day()
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 10);

            var period = new RentalPeriod(startDate, endDate);

            // Act
            var daysCount = period.DaysCount;

            // Assert - 10 days inclusive
            daysCount.ShouldBe(10);
        }

        [Fact]
        public void OverlapsWith_Should_Return_True_When_Overlapping()
        {
            // Arrange
            var period1Start = DateTime.Today;
            var period1End = DateTime.Today.AddDays(6);
            var period = new RentalPeriod(period1Start, period1End);

            var period2Start = DateTime.Today.AddDays(3);
            var period2End = DateTime.Today.AddDays(10);

            // Act
            var overlaps = period.OverlapsWith(period2Start, period2End);

            // Assert
            overlaps.ShouldBeTrue();
        }

        [Fact]
        public void OverlapsWith_Should_Return_False_When_Not_Overlapping()
        {
            // Arrange
            var period1Start = DateTime.Today;
            var period1End = DateTime.Today.AddDays(6);
            var period = new RentalPeriod(period1Start, period1End);

            var period2Start = DateTime.Today.AddDays(10);
            var period2End = DateTime.Today.AddDays(16);

            // Act
            var overlaps = period.OverlapsWith(period2Start, period2End);

            // Assert
            overlaps.ShouldBeFalse();
        }

        [Fact]
        public void OverlapsWith_Should_Return_True_When_Adjacent()
        {
            // Arrange - periods are adjacent (period1 ends when period2 starts)
            var period1Start = DateTime.Today;
            var period1End = DateTime.Today.AddDays(6);
            var period = new RentalPeriod(period1Start, period1End);

            var period2Start = period1End.AddDays(1);
            var period2End = period2Start.AddDays(6);

            // Act
            var overlaps = period.OverlapsWith(period2Start, period2End);

            // Assert - adjacent periods should not overlap
            overlaps.ShouldBeFalse();
        }

        [Fact]
        public void OverlapsWith_Should_Return_True_For_Contained_Period()
        {
            // Arrange
            var period1Start = DateTime.Today;
            var period1End = DateTime.Today.AddDays(10);
            var period = new RentalPeriod(period1Start, period1End);

            var period2Start = DateTime.Today.AddDays(2);
            var period2End = DateTime.Today.AddDays(5);

            // Act
            var overlaps = period.OverlapsWith(period2Start, period2End);

            // Assert - contained period should overlap
            overlaps.ShouldBeTrue();
        }

        [Fact]
        public void Contains_Should_Return_True_When_Date_In_Period()
        {
            // Arrange
            var startDate = DateTime.Today;
            var endDate = DateTime.Today.AddDays(6);
            var period = new RentalPeriod(startDate, endDate);
            var dateInPeriod = DateTime.Today.AddDays(3);

            // Act
            var contains = period.Contains(dateInPeriod);

            // Assert
            contains.ShouldBeTrue();
        }

        [Fact]
        public void Contains_Should_Return_True_For_Start_Date()
        {
            // Arrange
            var startDate = DateTime.Today;
            var endDate = DateTime.Today.AddDays(6);
            var period = new RentalPeriod(startDate, endDate);

            // Act
            var contains = period.Contains(startDate);

            // Assert
            contains.ShouldBeTrue();
        }

        [Fact]
        public void Contains_Should_Return_True_For_End_Date()
        {
            // Arrange
            var startDate = DateTime.Today;
            var endDate = DateTime.Today.AddDays(6);
            var period = new RentalPeriod(startDate, endDate);

            // Act
            var contains = period.Contains(endDate);

            // Assert
            contains.ShouldBeTrue();
        }

        [Fact]
        public void Contains_Should_Return_False_When_Date_Before_Period()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(5);
            var endDate = DateTime.Today.AddDays(10);
            var period = new RentalPeriod(startDate, endDate);
            var dateBeforePeriod = DateTime.Today;

            // Act
            var contains = period.Contains(dateBeforePeriod);

            // Assert
            contains.ShouldBeFalse();
        }

        [Fact]
        public void Contains_Should_Return_False_When_Date_After_Period()
        {
            // Arrange
            var startDate = DateTime.Today;
            var endDate = DateTime.Today.AddDays(6);
            var period = new RentalPeriod(startDate, endDate);
            var dateAfterPeriod = DateTime.Today.AddDays(10);

            // Act
            var contains = period.Contains(dateAfterPeriod);

            // Assert
            contains.ShouldBeFalse();
        }

        [Fact]
        public void IsActive_Should_Return_True_When_Current_Date_In_Period()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(-1);
            var endDate = DateTime.Today.AddDays(5);
            var period = new RentalPeriod(startDate, endDate);

            // Act
            var isActive = period.IsActive();

            // Assert
            isActive.ShouldBeTrue();
        }

        [Fact]
        public void IsActive_Should_Return_False_When_Period_In_Past()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(-10);
            var endDate = DateTime.Today.AddDays(-5);
            var period = new RentalPeriod(startDate, endDate);

            // Act
            var isActive = period.IsActive();

            // Assert
            isActive.ShouldBeFalse();
        }

        [Fact]
        public void IsActive_Should_Return_False_When_Period_In_Future()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(5);
            var endDate = DateTime.Today.AddDays(10);
            var period = new RentalPeriod(startDate, endDate);

            // Act
            var isActive = period.IsActive();

            // Assert
            isActive.ShouldBeFalse();
        }

        [Fact]
        public void IsFuture_Should_Return_True_When_StartDate_After_Today()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(5);
            var endDate = DateTime.Today.AddDays(10);
            var period = new RentalPeriod(startDate, endDate);

            // Act
            var isFuture = period.IsFuture();

            // Assert
            isFuture.ShouldBeTrue();
        }

        [Fact]
        public void IsFuture_Should_Return_False_When_StartDate_Is_Today()
        {
            // Arrange
            var startDate = DateTime.Today;
            var endDate = DateTime.Today.AddDays(5);
            var period = new RentalPeriod(startDate, endDate);

            // Act
            var isFuture = period.IsFuture();

            // Assert
            isFuture.ShouldBeFalse();
        }

        [Fact]
        public void IsFuture_Should_Return_False_When_StartDate_In_Past()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(-5);
            var endDate = DateTime.Today.AddDays(5);
            var period = new RentalPeriod(startDate, endDate);

            // Act
            var isFuture = period.IsFuture();

            // Assert
            isFuture.ShouldBeFalse();
        }
    }
}
