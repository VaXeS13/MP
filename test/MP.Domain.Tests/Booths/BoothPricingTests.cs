using System;
using System.Collections.Generic;
using System.Linq;
using MP.Domain.Booths;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace MP.Domain.Tests.Booths
{
    public class BoothPricingTests : MPDomainTestBase<MPDomainTestModule>
    {
        private static readonly Guid DefaultOrganizationalUnitId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        [Fact]
        public void Should_Create_Booth_With_Single_Pricing_Period()
        {
            // Arrange
            var boothId = Guid.NewGuid();
            var periods = new List<PricingPeriod>
            {
                new PricingPeriod(1, 10m, boothId) // 1 day = 10 PLN
            };

            // Act
            var booth = new Booth(boothId, "A-01", periods, DefaultOrganizationalUnitId);

            // Assert
            booth.PricingPeriods.ShouldNotBeEmpty();
            booth.PricingPeriods.Count.ShouldBe(1);
            booth.PricingPeriods[0].Days.ShouldBe(1);
            booth.PricingPeriods[0].PricePerPeriod.ShouldBe(10m);
        }

        [Fact]
        public void Should_Create_Booth_With_Multiple_Pricing_Periods()
        {
            // Arrange
            var boothId = Guid.NewGuid();
            var periods = new List<PricingPeriod>
            {
                new PricingPeriod(1, 5m, boothId),   // 1 day = 5 PLN
                new PricingPeriod(7, 30m, boothId),  // 7 days = 30 PLN
                new PricingPeriod(30, 100m, boothId) // 30 days = 100 PLN
            };

            // Act
            var booth = new Booth(boothId, "A-02", periods, DefaultOrganizationalUnitId);

            // Assert
            booth.PricingPeriods.Count.ShouldBe(3);
        }

        [Fact]
        public void Should_Throw_When_Creating_Booth_Without_Pricing_Periods()
        {
            // Arrange
            var boothId = Guid.NewGuid();
            var emptyPeriods = new List<PricingPeriod>();

            // Act & Assert
            var exception = Should.Throw<BusinessException>(() =>
            {
                new Booth(boothId, "A-03", emptyPeriods, DefaultOrganizationalUnitId);
            });

            exception.Code.ShouldBe("BOOTH_PRICING_PERIODS_REQUIRED");
        }

        [Fact]
        public void Should_Throw_When_Pricing_Period_Has_Duplicate_Days()
        {
            // Arrange
            var boothId = Guid.NewGuid();
            var periods = new List<PricingPeriod>
            {
                new PricingPeriod(7, 30m, boothId),
                new PricingPeriod(7, 35m, boothId) // Duplicate 7 days
            };

            // Act & Assert
            var exception = Should.Throw<BusinessException>(() =>
            {
                new Booth(boothId, "A-04", periods, DefaultOrganizationalUnitId);
            });

            exception.Code.ShouldBe("BOOTH_PRICING_PERIODS_MUST_BE_UNIQUE");
        }

        [Theory]
        [InlineData(1, 5, 5)]       // 1 day @ 5 PLN = 5 PLN
        [InlineData(7, 5, 35)]      // 7 days @ 5 PLN/day = 35 PLN
        [InlineData(14, 5, 70)]     // 14 days @ 5 PLN/day = 70 PLN
        public void Should_Calculate_Price_With_Single_Period(int days, decimal price, decimal expected)
        {
            // Arrange
            var boothId = Guid.NewGuid();
            var periods = new List<PricingPeriod>
            {
                new PricingPeriod(1, price, boothId)
            };
            var booth = new Booth(boothId, "A-05", periods, DefaultOrganizationalUnitId);

            // Act
            var result = booth.CalculatePrice(days);

            // Assert
            result.TotalPrice.ShouldBe(expected);
        }

        [Fact]
        public void Should_Calculate_Price_Using_Greedy_Algorithm_Example1()
        {
            // Arrange: 1 day = 1 PLN, 7 days = 6 PLN
            // Calculate: 16 days should be 2×7 days + 2×1 day = 12 + 2 = 14 PLN
            var boothId = Guid.NewGuid();
            var periods = new List<PricingPeriod>
            {
                new PricingPeriod(1, 1m, boothId),
                new PricingPeriod(7, 6m, boothId)
            };
            var booth = new Booth(boothId, "A-06", periods, DefaultOrganizationalUnitId);

            // Act
            var result = booth.CalculatePrice(16);

            // Assert
            result.TotalPrice.ShouldBe(14m);
            result.Breakdown.Count.ShouldBe(2);

            // First breakdown: 2 × 7 days
            var period7 = result.Breakdown.First(b => b.Days == 7);
            period7.Count.ShouldBe(2);
            period7.PricePerPeriod.ShouldBe(6m);
            period7.Subtotal.ShouldBe(12m);

            // Second breakdown: 2 × 1 day
            var period1 = result.Breakdown.First(b => b.Days == 1);
            period1.Count.ShouldBe(2);
            period1.PricePerPeriod.ShouldBe(1m);
            period1.Subtotal.ShouldBe(2m);
        }

        [Fact]
        public void Should_Calculate_Price_Using_Greedy_Algorithm_Example2()
        {
            // Arrange: 1 day = 5 PLN, 7 days = 30 PLN, 30 days = 100 PLN
            // Calculate: 45 days should be 1×30 days + 2×7 days + 1×1 day = 100 + 60 + 5 = 165 PLN
            var boothId = Guid.NewGuid();
            var periods = new List<PricingPeriod>
            {
                new PricingPeriod(1, 5m, boothId),
                new PricingPeriod(7, 30m, boothId),
                new PricingPeriod(30, 100m, boothId)
            };
            var booth = new Booth(boothId, "A-07", periods, DefaultOrganizationalUnitId);

            // Act
            var result = booth.CalculatePrice(45);

            // Assert
            result.TotalPrice.ShouldBe(165m);
            result.Breakdown.Count.ShouldBe(3);
        }

        [Fact]
        public void Should_Calculate_Price_With_Remainder_Days()
        {
            // Arrange: Only 7-day period available
            // Calculate: 10 days should be 1×7 days + 1×7 days (ceiling) = 14 days charged
            var boothId = Guid.NewGuid();
            var periods = new List<PricingPeriod>
            {
                new PricingPeriod(7, 30m, boothId)
            };
            var booth = new Booth(boothId, "A-08", periods, DefaultOrganizationalUnitId);

            // Act
            var result = booth.CalculatePrice(10);

            // Assert
            // Should use 2 × 7-day periods (14 days total) because no smaller period available
            // Algorithm adds each period usage separately, so we get 2 breakdown items
            result.Breakdown.Count.ShouldBeGreaterThanOrEqualTo(1);
            var total7DayPeriods = result.Breakdown.Where(b => b.Days == 7).Sum(b => b.Count);
            total7DayPeriods.ShouldBe(2);
            result.TotalPrice.ShouldBe(60m);
        }

        [Fact]
        public void Should_Throw_When_Calculating_Price_For_Zero_Days()
        {
            // Arrange
            var boothId = Guid.NewGuid();
            var periods = new List<PricingPeriod>
            {
                new PricingPeriod(1, 5m, boothId)
            };
            var booth = new Booth(boothId, "A-09", periods, DefaultOrganizationalUnitId);

            // Act & Assert
            var exception = Should.Throw<BusinessException>(() =>
            {
                booth.CalculatePrice(0);
            });

            exception.Code.ShouldBe("RENTAL_DAYS_MUST_BE_POSITIVE");
        }

        [Fact]
        public void Should_Throw_When_Calculating_Price_For_Negative_Days()
        {
            // Arrange
            var boothId = Guid.NewGuid();
            var periods = new List<PricingPeriod>
            {
                new PricingPeriod(1, 5m, boothId)
            };
            var booth = new Booth(boothId, "A-10", periods, DefaultOrganizationalUnitId);

            // Act & Assert
            var exception = Should.Throw<BusinessException>(() =>
            {
                booth.CalculatePrice(-5);
            });

            exception.Code.ShouldBe("RENTAL_DAYS_MUST_BE_POSITIVE");
        }

        [Fact]
        public void PricingPeriod_Should_Calculate_Price_Per_Day()
        {
            // Arrange & Act
            var period = new PricingPeriod(7, 35m, Guid.NewGuid());

            // Assert
            period.GetPricePerDay().ShouldBe(5m); // 35 PLN / 7 days = 5 PLN/day
        }

        [Fact]
        public void Should_Update_Legacy_PricePerDay_When_Setting_Pricing_Periods()
        {
            // Arrange
            var boothId = Guid.NewGuid();
            var booth = new Booth(boothId, "A-11", 10m, DefaultOrganizationalUnitId); // Old constructor

            var newPeriods = new List<PricingPeriod>
            {
                new PricingPeriod(1, 7m, boothId),
                new PricingPeriod(7, 40m, boothId)
            };

            // Act
            booth.SetPricingPeriods(newPeriods);

            // Assert
#pragma warning disable CS0618 // Type or member is obsolete
            booth.PricePerDay.ShouldBe(7m); // Should use 1-day period price
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Fact]
        public void Should_Calculate_Legacy_PricePerDay_From_Smallest_Period_When_No_1Day_Period()
        {
            // Arrange
            var boothId = Guid.NewGuid();
            var booth = new Booth(boothId, "A-12", 10m, DefaultOrganizationalUnitId); // Old constructor

            var newPeriods = new List<PricingPeriod>
            {
                new PricingPeriod(7, 35m, boothId),   // 5 PLN/day
                new PricingPeriod(14, 60m, boothId)   // ~4.29 PLN/day
            };

            // Act
            booth.SetPricingPeriods(newPeriods);

            // Assert
#pragma warning disable CS0618 // Type or member is obsolete
            booth.PricePerDay.ShouldBe(5m); // 35 / 7 = 5 PLN/day
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
