using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MP.Promotions;
using Shouldly;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Application.Tests.Promotions
{
    public class PromotionAppServiceSimpleTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly IPromotionAppService _promotionAppService;

        public PromotionAppServiceSimpleTests()
        {
            _promotionAppService = GetRequiredService<IPromotionAppService>();
        }

        [Fact]
        [UnitOfWork]
        public async Task CreateAsync_Should_Create_Promotion()
        {
            // Arrange
            var name = $"Promo_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var createDto = new CreatePromotionDto
            {
                Name = name,
                Description = "Test promotion",
                Type = PromotionType.Quantity,
                DisplayMode = PromotionDisplayMode.StickyBottomRight,
                DiscountType = DiscountType.Percentage,
                DiscountValue = 10m,
                Priority = 1
            };

            // Act
            var result = await _promotionAppService.CreateAsync(createDto);

            // Assert
            result.ShouldNotBeNull();
            result.Name.ShouldBe(name);
            result.Type.ShouldBe(PromotionType.Quantity);
            result.DiscountType.ShouldBe(DiscountType.Percentage);
            result.DiscountValue.ShouldBe(10m);
        }

        [Fact]
        [UnitOfWork]
        public async Task GetAsync_Should_Return_Promotion()
        {
            // Arrange
            var name = $"Promo_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _promotionAppService.CreateAsync(new CreatePromotionDto
            {
                Name = name,
                Type = PromotionType.PromoCode,
                DisplayMode = PromotionDisplayMode.StickyBottomLeft,
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 50m,
                Priority = 2
            });

            // Act
            var result = await _promotionAppService.GetAsync(created.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(created.Id);
            result.Name.ShouldBe(name);
            result.Type.ShouldBe(PromotionType.PromoCode);
        }

        [Fact]
        [UnitOfWork]
        public async Task UpdateAsync_Should_Update_Promotion()
        {
            // Arrange
            var originalName = $"Original_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _promotionAppService.CreateAsync(new CreatePromotionDto
            {
                Name = originalName,
                Type = PromotionType.Quantity,
                DisplayMode = PromotionDisplayMode.StickyBottomLeft,
                DiscountType = DiscountType.Percentage,
                DiscountValue = 5m,
                Priority = 1
            });

            var newName = $"Updated_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var updateDto = new UpdatePromotionDto
            {
                Name = newName,
                Type = PromotionType.DateRange,
                DisplayMode = PromotionDisplayMode.Popup,
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 100m,
                Priority = 3
            };

            // Act
            var result = await _promotionAppService.UpdateAsync(created.Id, updateDto);

            // Assert
            result.ShouldNotBeNull();
            result.Name.ShouldBe(newName);
            result.Type.ShouldBe(PromotionType.DateRange);
            result.DiscountType.ShouldBe(DiscountType.FixedAmount);
            result.DiscountValue.ShouldBe(100m);
        }

        [Fact]
        [UnitOfWork]
        public async Task GetListAsync_Should_Return_Paginated_Promotions()
        {
            // Arrange
            var name1 = $"Promo1_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var name2 = $"Promo2_{Guid.NewGuid().ToString().Substring(0, 8)}";

            await _promotionAppService.CreateAsync(new CreatePromotionDto
            {
                Name = name1,
                Type = PromotionType.Quantity,
                DisplayMode = PromotionDisplayMode.StickyBottomLeft,
                DiscountType = DiscountType.Percentage,
                DiscountValue = 10m,
                Priority = 1
            });

            await _promotionAppService.CreateAsync(new CreatePromotionDto
            {
                Name = name2,
                Type = PromotionType.NewUser,
                DisplayMode = PromotionDisplayMode.StickyBottomLeft,
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 25m,
                Priority = 2
            });

            // Act
            var result = await _promotionAppService.GetListAsync(new GetPromotionsInput());

            // Assert
            result.ShouldNotBeNull();
            result.Items.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        [UnitOfWork]
        public async Task ActivateAsync_Should_Activate_Promotion()
        {
            // Arrange
            var name = $"Promo_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _promotionAppService.CreateAsync(new CreatePromotionDto
            {
                Name = name,
                Type = PromotionType.Quantity,
                DisplayMode = PromotionDisplayMode.StickyBottomLeft,
                DiscountType = DiscountType.Percentage,
                DiscountValue = 15m,
                Priority = 1,
                IsActive = false
            });

            // Act
            var result = await _promotionAppService.ActivateAsync(created.Id);

            // Assert
            result.ShouldNotBeNull();
            result.IsActive.ShouldBeTrue();
        }

        [Fact]
        [UnitOfWork]
        public async Task DeactivateAsync_Should_Deactivate_Promotion()
        {
            // Arrange
            var name = $"Promo_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _promotionAppService.CreateAsync(new CreatePromotionDto
            {
                Name = name,
                Type = PromotionType.Quantity,
                DisplayMode = PromotionDisplayMode.StickyBottomLeft,
                DiscountType = DiscountType.Percentage,
                DiscountValue = 20m,
                Priority = 1,
                IsActive = true
            });

            // Act
            var result = await _promotionAppService.DeactivateAsync(created.Id);

            // Assert
            result.ShouldNotBeNull();
            result.IsActive.ShouldBeFalse();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetActivePromotionsAsync_Should_Return_Only_Active_Promotions()
        {
            // Arrange
            var activeName = $"Active_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var inactiveName = $"Inactive_{Guid.NewGuid().ToString().Substring(0, 8)}";

            var activePromoId = (await _promotionAppService.CreateAsync(new CreatePromotionDto
            {
                Name = activeName,
                Type = PromotionType.Quantity,
                DisplayMode = PromotionDisplayMode.StickyBottomLeft,
                DiscountType = DiscountType.Percentage,
                DiscountValue = 10m,
                Priority = 1,
                IsActive = true
            })).Id;

            var inactivePromoId = (await _promotionAppService.CreateAsync(new CreatePromotionDto
            {
                Name = inactiveName,
                Type = PromotionType.PromoCode,
                DisplayMode = PromotionDisplayMode.StickyBottomLeft,
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 50m,
                Priority = 2,
                IsActive = false
            })).Id;

            // Act
            var result = await _promotionAppService.GetActivePromotionsAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Any(p => p.Id == activePromoId).ShouldBeTrue();
            result.All(p => p.IsActive).ShouldBeTrue();
        }

        [Fact]
        [UnitOfWork]
        public async Task DeleteAsync_Should_Delete_Promotion()
        {
            // Arrange
            var name = $"Promo_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _promotionAppService.CreateAsync(new CreatePromotionDto
            {
                Name = name,
                Type = PromotionType.Quantity,
                DisplayMode = PromotionDisplayMode.StickyBottomLeft,
                DiscountType = DiscountType.Percentage,
                DiscountValue = 5m,
                Priority = 1
            });

            // Act
            await _promotionAppService.DeleteAsync(created.Id);

            // Assert
            var exception = await Should.ThrowAsync<Exception>(
                () => _promotionAppService.GetAsync(created.Id)
            );
            exception.ShouldNotBeNull();
        }
    }
}
