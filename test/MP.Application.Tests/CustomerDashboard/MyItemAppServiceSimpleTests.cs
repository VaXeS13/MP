using System.Threading.Tasks;
using MP.Application.Contracts.CustomerDashboard;
using Shouldly;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Application.Tests.CustomerDashboard
{
    public class MyItemAppServiceSimpleTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly IMyItemAppService _myItemAppService;

        public MyItemAppServiceSimpleTests()
        {
            _myItemAppService = GetRequiredService<IMyItemAppService>();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetMyItemsAsync_Should_Return_Items()
        {
            // Arrange
            var input = new GetMyItemsDto();

            // Act
            var result = await _myItemAppService.GetMyItemsAsync(input);

            // Assert
            result.ShouldNotBeNull();
        }
    }
}
