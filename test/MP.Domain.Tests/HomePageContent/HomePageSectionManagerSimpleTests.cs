using System;
using System.Threading.Tasks;
using MP.Domain.HomePageContent;
using MP.HomePageContent;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Domain.Tests.HomePageContent
{
    public class HomePageSectionManagerSimpleTests : MPDomainTestBase<MPDomainTestModule>
    {
        private readonly HomePageSectionManager _sectionManager;
        private readonly IHomePageSectionRepository _sectionRepository;

        public HomePageSectionManagerSimpleTests()
        {
            _sectionManager = GetRequiredService<HomePageSectionManager>();
            _sectionRepository = GetRequiredService<IHomePageSectionRepository>();
        }

        [Fact]
        [UnitOfWork]
        public async Task CreateAsync_Should_Increment_Order()
        {
            // Arrange
            var section1 = await _sectionManager.CreateAsync(
                HomePageSectionType.HeroBanner,
                $"Sec1_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Guid.NewGuid()
            );

            // Act
            var section2 = await _sectionManager.CreateAsync(
                HomePageSectionType.PromotionCards,
                $"Sec2_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Guid.NewGuid()
            );

            // Assert
            section2.Order.ShouldBe(section1.Order + 1);
        }

        [Fact]
        [UnitOfWork]
        public async Task UpdateAsync_Should_Update_Section()
        {
            // Arrange
            var section = await _sectionManager.CreateAsync(
                HomePageSectionType.HeroBanner,
                $"Original_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Guid.NewGuid()
            );
            var newTitle = $"Updated_{Guid.NewGuid().ToString().Substring(0, 8)}";

            // Act
            await _sectionManager.UpdateAsync(
                section,
                HomePageSectionType.FeatureHighlights,
                newTitle,
                "New Subtitle"
            );

            // Assert
            section.Title.ShouldBe(newTitle);
            section.SectionType.ShouldBe(HomePageSectionType.FeatureHighlights);
        }

        [Fact]
        [UnitOfWork]
        public async Task ReorderAsync_Should_Change_Order()
        {
            // Arrange
            var section = await _sectionManager.CreateAsync(
                HomePageSectionType.HeroBanner,
                $"Sec_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Guid.NewGuid()
            );
            var newOrder = 5;

            // Act
            await _sectionManager.ReorderAsync(section.Id, newOrder);

            // Assert
            var updated = await _sectionRepository.GetAsync(section.Id);
            updated.Order.ShouldBe(newOrder);
        }

        [Fact]
        [UnitOfWork]
        public async Task ReorderAsync_Should_Throw_When_Negative_Order()
        {
            // Arrange
            var section = await _sectionManager.CreateAsync(
                HomePageSectionType.HeroBanner,
                $"Sec_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Guid.NewGuid()
            );

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _sectionManager.ReorderAsync(section.Id, -1)
            );

            exception.Code.ShouldBe("HOMEPAGE_SECTION_ORDER_CANNOT_BE_NEGATIVE");
        }
    }
}
