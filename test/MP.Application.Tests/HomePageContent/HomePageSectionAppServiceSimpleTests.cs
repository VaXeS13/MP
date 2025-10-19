using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MP.Application.Contracts.HomePageContent;
using MP.HomePageContent;
using Shouldly;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Application.Tests.HomePageContent
{
    public class HomePageSectionAppServiceSimpleTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly IHomePageSectionAppService _homePageSectionAppService;

        public HomePageSectionAppServiceSimpleTests()
        {
            _homePageSectionAppService = GetRequiredService<IHomePageSectionAppService>();
        }

        [Fact]
        [UnitOfWork]
        public async Task CreateAsync_Should_Create_HomePageSection()
        {
            // Arrange
            var title = $"Section_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var createDto = new CreateHomePageSectionDto
            {
                SectionType = HomePageSectionType.HeroBanner,
                Title = title,
                Subtitle = "Test subtitle",
                Content = "Test content"
            };

            // Act
            var result = await _homePageSectionAppService.CreateAsync(createDto);

            // Assert
            result.ShouldNotBeNull();
            result.Title.ShouldBe(title);
            result.SectionType.ShouldBe(HomePageSectionType.HeroBanner);
            result.Subtitle.ShouldBe("Test subtitle");
            result.Content.ShouldBe("Test content");
        }

        [Fact]
        [UnitOfWork]
        public async Task GetAsync_Should_Return_Section()
        {
            // Arrange
            var title = $"Section_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _homePageSectionAppService.CreateAsync(new CreateHomePageSectionDto
            {
                SectionType = HomePageSectionType.PromotionCards,
                Title = title,
                Subtitle = "Promo"
            });

            // Act
            var result = await _homePageSectionAppService.GetAsync(created.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(created.Id);
            result.Title.ShouldBe(title);
            result.SectionType.ShouldBe(HomePageSectionType.PromotionCards);
        }

        [Fact]
        [UnitOfWork]
        public async Task UpdateAsync_Should_Update_Section()
        {
            // Arrange
            var originalTitle = $"Original_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _homePageSectionAppService.CreateAsync(new CreateHomePageSectionDto
            {
                SectionType = HomePageSectionType.HeroBanner,
                Title = originalTitle
            });

            var newTitle = $"Updated_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var updateDto = new UpdateHomePageSectionDto
            {
                SectionType = HomePageSectionType.FeatureHighlights,
                Title = newTitle,
                Subtitle = "Updated subtitle"
            };

            // Act
            await _homePageSectionAppService.UpdateAsync(created.Id, updateDto);

            // Assert
            var updated = await _homePageSectionAppService.GetAsync(created.Id);
            updated.Title.ShouldBe(newTitle);
            updated.SectionType.ShouldBe(HomePageSectionType.FeatureHighlights);
            updated.Subtitle.ShouldBe("Updated subtitle");
        }

        [Fact]
        [UnitOfWork]
        public async Task GetListAsync_Should_Return_Paginated_Sections()
        {
            // Arrange
            var title1 = $"Section1_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var title2 = $"Section2_{Guid.NewGuid().ToString().Substring(0, 8)}";

            await _homePageSectionAppService.CreateAsync(new CreateHomePageSectionDto
            {
                SectionType = HomePageSectionType.HeroBanner,
                Title = title1
            });

            await _homePageSectionAppService.CreateAsync(new CreateHomePageSectionDto
            {
                SectionType = HomePageSectionType.PromotionCards,
                Title = title2
            });

            // Act
            var result = await _homePageSectionAppService.GetListAsync(
                new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto()
            );

            // Assert
            result.ShouldNotBeNull();
            result.Items.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        [UnitOfWork]
        public async Task GetAllOrderedAsync_Should_Return_All_Sections_Ordered()
        {
            // Arrange
            var title = $"Section_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _homePageSectionAppService.CreateAsync(new CreateHomePageSectionDto
            {
                SectionType = HomePageSectionType.HeroBanner,
                Title = title
            });

            // Act
            var result = await _homePageSectionAppService.GetAllOrderedAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBeGreaterThan(0);
            // Verify it's ordered by Order property
            for (int i = 0; i < result.Count - 1; i++)
            {
                result[i].Order.ShouldBeLessThanOrEqualTo(result[i + 1].Order);
            }
        }

        [Fact]
        [UnitOfWork]
        public async Task ActivateAsync_Should_Activate_Section()
        {
            // Arrange
            var title = $"Section_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _homePageSectionAppService.CreateAsync(new CreateHomePageSectionDto
            {
                SectionType = HomePageSectionType.HeroBanner,
                Title = title
            });

            // Act
            var result = await _homePageSectionAppService.ActivateAsync(created.Id);

            // Assert
            result.ShouldNotBeNull();
            result.IsActive.ShouldBeTrue();
        }

        [Fact]
        [UnitOfWork]
        public async Task DeactivateAsync_Should_Deactivate_Section()
        {
            // Arrange
            var title = $"Section_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _homePageSectionAppService.CreateAsync(new CreateHomePageSectionDto
            {
                SectionType = HomePageSectionType.HeroBanner,
                Title = title
            });

            await _homePageSectionAppService.ActivateAsync(created.Id);

            // Act
            var result = await _homePageSectionAppService.DeactivateAsync(created.Id);

            // Assert
            result.ShouldNotBeNull();
            result.IsActive.ShouldBeFalse();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetActiveForDisplayAsync_Should_Return_Only_Active_Sections()
        {
            // Arrange
            var activeTitle = $"Active_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var inactiveTitle = $"Inactive_{Guid.NewGuid().ToString().Substring(0, 8)}";

            var activeSectionId = (await _homePageSectionAppService.CreateAsync(new CreateHomePageSectionDto
            {
                SectionType = HomePageSectionType.HeroBanner,
                Title = activeTitle
            })).Id;

            var inactiveSectionId = (await _homePageSectionAppService.CreateAsync(new CreateHomePageSectionDto
            {
                SectionType = HomePageSectionType.PromotionCards,
                Title = inactiveTitle
            })).Id;

            await _homePageSectionAppService.ActivateAsync(activeSectionId);

            // Act
            var result = await _homePageSectionAppService.GetActiveForDisplayAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Any(s => s.Id == activeSectionId).ShouldBeTrue();
            result.All(s => s.IsActive).ShouldBeTrue();
        }

        [Fact]
        [UnitOfWork]
        public async Task DeleteAsync_Should_Delete_Section()
        {
            // Arrange
            var title = $"Section_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _homePageSectionAppService.CreateAsync(new CreateHomePageSectionDto
            {
                SectionType = HomePageSectionType.HeroBanner,
                Title = title
            });

            // Act
            await _homePageSectionAppService.DeleteAsync(created.Id);

            // Assert
            var exception = await Should.ThrowAsync<Exception>(
                () => _homePageSectionAppService.GetAsync(created.Id)
            );
            exception.ShouldNotBeNull();
        }
    }
}
