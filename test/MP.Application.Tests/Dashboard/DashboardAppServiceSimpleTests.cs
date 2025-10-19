using System;
using System.Threading.Tasks;
using MP.Application.Contracts.Dashboard;
using Shouldly;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Application.Tests.Dashboard
{
    public class DashboardAppServiceSimpleTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly IDashboardAppService _dashboardAppService;

        public DashboardAppServiceSimpleTests()
        {
            _dashboardAppService = GetRequiredService<IDashboardAppService>();
        }

        private PeriodFilterDto CreatePeriodFilter()
        {
            return new PeriodFilterDto
            {
                StartDate = DateTime.Today.AddDays(-30),
                EndDate = DateTime.Today
            };
        }

        [Fact]
        [UnitOfWork]
        public async Task GetOverviewAsync_Should_Return_Dashboard_Overview()
        {
            // Arrange
            var filter = CreatePeriodFilter();

            // Act
            var result = await _dashboardAppService.GetOverviewAsync(filter);

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetSalesAnalyticsAsync_Should_Return_Sales_Analytics()
        {
            // Arrange
            var filter = CreatePeriodFilter();

            // Act
            var result = await _dashboardAppService.GetSalesAnalyticsAsync(filter);

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetBoothOccupancyAsync_Should_Return_Booth_Occupancy()
        {
            // Arrange
            var filter = CreatePeriodFilter();

            // Act
            var result = await _dashboardAppService.GetBoothOccupancyAsync(filter);

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetFinancialReportsAsync_Should_Return_Financial_Overview()
        {
            // Arrange
            var filter = CreatePeriodFilter();

            // Act
            var result = await _dashboardAppService.GetFinancialReportsAsync(filter);

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetCustomerAnalyticsAsync_Should_Return_Customer_Analytics()
        {
            // Arrange
            var filter = CreatePeriodFilter();

            // Act
            var result = await _dashboardAppService.GetCustomerAnalyticsAsync(filter);

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetPaymentAnalyticsAsync_Should_Return_Payment_Analytics()
        {
            // Arrange
            var filter = CreatePeriodFilter();

            // Act
            var result = await _dashboardAppService.GetPaymentAnalyticsAsync(filter);

            // Assert
            result.ShouldNotBeNull();
        }
    }
}
