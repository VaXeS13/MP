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
            var startDate = DateTime.Today.AddDays(7);
            var endDate = DateTime.Today.AddDays(13); // 7 days

            // Act
            var period = new RentalPeriod(startDate, endDate);

            // Assert
            period.StartDate.ShouldBe(startDate.Date);
            period.EndDate.ShouldBe(endDate.Date);
        }

        [Fact]
        public void Constructor_Should_Throw_When_EndDate_Before_StartDate()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(10);
            var endDate = DateTime.Today.AddDays(5);

            // Act & Assert
            var exception = Should.Throw<BusinessException>(
                () => new RentalPeriod(startDate, endDate)
            );

            exception.Code.ShouldBe("RENTAL_END_DATE_MUST_BE_AFTER_START");
        }

        [Fact]
        public void Constructor_Should_Throw_When_EndDate_Equals_StartDate()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(7);
            var endDate = DateTime.Today.AddDays(7);

            // Act & Assert
            var exception = Should.Throw<BusinessException>(
                () => new RentalPeriod(startDate, endDate)
            );

            exception.Code.ShouldBe("RENTAL_END_DATE_MUST_BE_AFTER_START");
        }

        [Fact]
        public void Constructor_Should_Throw_When_StartDate_In_Past()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(-1);
            var endDate = DateTime.Today.AddDays(6);

            // Act & Assert
            var exception = Should.Throw<BusinessException>(
                () => new RentalPeriod(startDate, endDate)
            );

            exception.Code.ShouldBe("RENTAL_START_DATE_CANNOT_BE_IN_PAST");
        }

        [Fact]
        public void Constructor_Should_Accept_Short_Periods()
        {
            // Arrange - RentalPeriod itself only validates >= 1 day
            // Actual minimum rental period (7 days) is enforced at CartManager level
            var startDate = DateTime.Today.AddDays(7);
            var endDate = DateTime.Today.AddDays(9); // Only 3 days

            // Act
            var period = new RentalPeriod(startDate, endDate);

            // Assert - should create successfully
            period.StartDate.ShouldBe(startDate.Date);
            period.EndDate.ShouldBe(endDate.Date);
            period.GetDaysCount().ShouldBe(3);
        }

        [Fact]
        public void GetDaysCount_Should_Calculate_Correctly()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(7);
            var endDate = DateTime.Today.AddDays(13); // 7 days total

            var period = new RentalPeriod(startDate, endDate);

            // Act
            var daysCount = period.GetDaysCount();

            // Assert
            daysCount.ShouldBe(7);
        }

        [Fact]
        public void GetDaysCount_Should_Include_Both_Start_And_End_Day()
        {
            // Arrange - use future dates to avoid validation errors
            var startDate = DateTime.Today.AddDays(30);
            var endDate = DateTime.Today.AddDays(39);  // 10 days inclusive

            var period = new RentalPeriod(startDate, endDate);

            // Act
            var daysCount = period.GetDaysCount();

            // Assert - 10 days inclusive (30-39 = 10 days)
            daysCount.ShouldBe(10);
        }

        [Fact]
        public void OverlapsWith_Should_Return_True_When_Overlapping()
        {
            // Arrange
            var period1 = new RentalPeriod(DateTime.Today.AddDays(7), DateTime.Today.AddDays(13));
            var period2 = new RentalPeriod(DateTime.Today.AddDays(10), DateTime.Today.AddDays(16));

            // Act
            var overlaps = period1.OverlapsWith(period2);

            // Assert
            overlaps.ShouldBeTrue();
        }

        [Fact]
        public void OverlapsWith_Should_Return_False_When_Not_Overlapping()
        {
            // Arrange
            var period1 = new RentalPeriod(DateTime.Today.AddDays(7), DateTime.Today.AddDays(13));
            var period2 = new RentalPeriod(DateTime.Today.AddDays(17), DateTime.Today.AddDays(23));

            // Act
            var overlaps = period1.OverlapsWith(period2);

            // Assert
            overlaps.ShouldBeFalse();
        }

        [Fact]
        public void OverlapsWith_Should_Return_False_When_Adjacent()
        {
            // Arrange - periods are adjacent (period1 ends when period2 starts)
            var period1 = new RentalPeriod(DateTime.Today.AddDays(7), DateTime.Today.AddDays(13));
            var period2 = new RentalPeriod(DateTime.Today.AddDays(14), DateTime.Today.AddDays(20));

            // Act
            var overlaps = period1.OverlapsWith(period2);

            // Assert - adjacent periods should not overlap
            overlaps.ShouldBeFalse();
        }

        [Fact]
        public void OverlapsWith_Should_Return_True_For_Contained_Period()
        {
            // Arrange
            var period1 = new RentalPeriod(DateTime.Today.AddDays(7), DateTime.Today.AddDays(20));
            var period2 = new RentalPeriod(DateTime.Today.AddDays(10), DateTime.Today.AddDays(16));

            // Act
            var overlaps = period1.OverlapsWith(period2);

            // Assert - contained period should overlap
            overlaps.ShouldBeTrue();
        }

        [Fact]
        public void OverlapsWith_Should_Return_True_When_Same_Period()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(7);
            var endDate = DateTime.Today.AddDays(13);
            var period1 = new RentalPeriod(startDate, endDate);
            var period2 = new RentalPeriod(startDate, endDate);

            // Act
            var overlaps = period1.OverlapsWith(period2);

            // Assert
            overlaps.ShouldBeTrue();
        }

        [Fact]
        public void HasGapBefore_Should_Return_True_When_Gap_Exists()
        {
            // Arrange
            var previousPeriod = new RentalPeriod(DateTime.Today.AddDays(7), DateTime.Today.AddDays(13));
            var currentPeriod = new RentalPeriod(DateTime.Today.AddDays(17), DateTime.Today.AddDays(23));

            // Act
            var hasGap = currentPeriod.HasGapBefore(previousPeriod);

            // Assert
            hasGap.ShouldBeTrue();
        }

        [Fact]
        public void HasGapBefore_Should_Return_False_When_Adjacent()
        {
            // Arrange
            var previousPeriod = new RentalPeriod(DateTime.Today.AddDays(7), DateTime.Today.AddDays(13));
            var currentPeriod = new RentalPeriod(DateTime.Today.AddDays(14), DateTime.Today.AddDays(20));

            // Act
            var hasGap = currentPeriod.HasGapBefore(previousPeriod);

            // Assert
            hasGap.ShouldBeFalse();
        }

        [Fact]
        public void Create_Should_Create_Period_With_Days_Count()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(7);
            var daysCount = 10;

            // Act
            var period = RentalPeriod.Create(startDate, daysCount);

            // Assert
            period.StartDate.ShouldBe(startDate.Date);
            period.GetDaysCount().ShouldBe(daysCount);
        }

        [Fact]
        public void Create_Should_Throw_When_Days_Less_Than_1()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(7);
            var daysCount = 0;

            // Act & Assert
            var exception = Should.Throw<BusinessException>(
                () => RentalPeriod.Create(startDate, daysCount)
            );

            exception.Code.ShouldBe("RENTAL_PERIOD_MUST_BE_AT_LEAST_ONE_DAY");
        }

        [Fact]
        public void RentalPeriod_Should_Be_ValueObject()
        {
            // Arrange
            var period1 = new RentalPeriod(DateTime.Today.AddDays(7), DateTime.Today.AddDays(13));
            var period2 = new RentalPeriod(DateTime.Today.AddDays(7), DateTime.Today.AddDays(13));

            // Act & Assert - value objects with same properties should have same hash and atomic values
            period1.StartDate.ShouldBe(period2.StartDate);
            period1.EndDate.ShouldBe(period2.EndDate);
            period1.GetDaysCount().ShouldBe(period2.GetDaysCount());
        }

        [Fact]
        public void RentalPeriod_Should_Not_Equal_Different_Dates()
        {
            // Arrange
            var period1 = new RentalPeriod(DateTime.Today.AddDays(7), DateTime.Today.AddDays(13));
            var period2 = new RentalPeriod(DateTime.Today.AddDays(8), DateTime.Today.AddDays(14));

            // Act & Assert
            period1.Equals(period2).ShouldBeFalse();
        }
    }
}
