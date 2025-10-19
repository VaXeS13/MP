using System.Threading.Tasks;
using MP.Account;
using Shouldly;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Application.Tests.Account
{
    public class UserProfileAppServiceSimpleTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly IUserProfileAppService _userProfileAppService;

        public UserProfileAppServiceSimpleTests()
        {
            _userProfileAppService = GetRequiredService<IUserProfileAppService>();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetAsync_Should_Return_User_Profile()
        {
            // Act
            var result = await _userProfileAppService.GetAsync();

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task UpdateAsync_Should_Update_User_Profile()
        {
            // Arrange
            var profile = await _userProfileAppService.GetAsync();
            var updateDto = new UserProfileDto
            {
                Name = profile.Name,
                Surname = profile.Surname,
                Email = profile.Email
            };

            // Act
            var result = await _userProfileAppService.UpdateAsync(updateDto);

            // Assert
            result.ShouldNotBeNull();
        }
    }
}
