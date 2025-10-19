using System.Threading.Tasks;
using MP.Application.Contracts.CustomerDashboard;
using Shouldly;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Application.Tests.CustomerDashboard
{
    public class MyRentalAppServiceSimpleTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly IMyRentalAppService _myRentalAppService;

        public MyRentalAppServiceSimpleTests()
        {
            _myRentalAppService = GetRequiredService<IMyRentalAppService>();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetMyRentalsAsync_Should_Return_Rentals()
        {
            // Arrange
            var input = new GetMyRentalsDto();

            // Act
            var result = await _myRentalAppService.GetMyRentalsAsync(input);

            // Assert
            result.ShouldNotBeNull();
        }
    }
}
