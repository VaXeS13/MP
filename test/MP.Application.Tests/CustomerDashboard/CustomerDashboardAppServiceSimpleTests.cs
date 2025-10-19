using System.Threading.Tasks;
using MP.Application.Contracts.CustomerDashboard;
using Shouldly;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Application.Tests.CustomerDashboard
{
    public class CustomerDashboardAppServiceSimpleTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly ICustomerDashboardAppService _customerDashboardAppService;

        public CustomerDashboardAppServiceSimpleTests()
        {
            _customerDashboardAppService = GetRequiredService<ICustomerDashboardAppService>();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetDashboardAsync_Should_Return_Dashboard()
        {
            // Act
            var result = await _customerDashboardAppService.GetDashboardAsync();

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetSalesStatisticsAsync_Should_Return_Statistics()
        {
            // Arrange
            var filter = new CustomerStatisticsFilterDto();

            // Act
            var result = await _customerDashboardAppService.GetSalesStatisticsAsync(filter);

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetMyActiveRentalsAsync_Should_Return_Active_Rentals()
        {
            // Act
            var result = await _customerDashboardAppService.GetMyActiveRentalsAsync();

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetMySettlementsAsync_Should_Return_Settlements()
        {
            // Arrange
            var input = new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto();

            // Act
            var result = await _customerDashboardAppService.GetMySettlementsAsync(input);

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetSettlementSummaryAsync_Should_Return_Summary()
        {
            // Act
            var result = await _customerDashboardAppService.GetSettlementSummaryAsync();

            // Assert
            result.ShouldNotBeNull();
        }
    }
}
