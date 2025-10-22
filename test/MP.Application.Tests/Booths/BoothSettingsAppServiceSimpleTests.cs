using System.Threading.Tasks;
using MP.Booths;
using Shouldly;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Application.Tests.Booths
{
    public class BoothSettingsAppServiceSimpleTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly IBoothSettingsAppService _boothSettingsAppService;

        public BoothSettingsAppServiceSimpleTests()
        {
            _boothSettingsAppService = GetRequiredService<IBoothSettingsAppService>();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetAsync_Should_Return_Booth_Settings()
        {
            // Act
            var result = await _boothSettingsAppService.GetAsync();

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task UpdateAsync_Should_Update_Settings()
        {
            // Arrange
            var settings = await _boothSettingsAppService.GetAsync();
            settings.ShouldNotBeNull();

            var updateDto = new BoothSettingsDto
            {
                MinimumGapDays = 7,
                MinimumRentalDays = 7
            };

            // Act
            await _boothSettingsAppService.UpdateAsync(updateDto);

            // Assert - verify it was updated
            var updated = await _boothSettingsAppService.GetAsync();
            updated.ShouldNotBeNull();
            updated.MinimumGapDays.ShouldBe(7);
            updated.MinimumRentalDays.ShouldBe(7);
        }
    }
}
