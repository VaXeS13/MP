using System;
using System.Linq;
using System.Threading.Tasks;
using MP.Items;
using MP.Domain.Booths;
using Shouldly;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Application.Tests.Items
{
    public class ItemAppServiceSimpleTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly IItemAppService _itemAppService;

        public ItemAppServiceSimpleTests()
        {
            _itemAppService = GetRequiredService<IItemAppService>();
        }

        [Fact]
        [UnitOfWork]
        public async Task CreateAsync_Should_Create_Item()
        {
            // Arrange
            var itemName = $"Item_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var createItemDto = new CreateItemDto
            {
                Name = itemName,
                Price = 100m,
                Category = "TestCategory"
            };

            // Act
            var item = await _itemAppService.CreateAsync(createItemDto);

            // Assert
            item.ShouldNotBeNull();
            item.Name.ShouldBe(itemName);
            item.Price.ShouldBe(100m);
            item.Category.ShouldBe("TestCategory");
        }

        [Fact]
        [UnitOfWork]
        public async Task GetMyItemsAsync_Should_Return_User_Items()
        {
            // Arrange
            var itemName = $"Item_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var createItemDto = new CreateItemDto
            {
                Name = itemName,
                Price = 100m
            };

            await _itemAppService.CreateAsync(createItemDto);

            // Act
            var result = await _itemAppService.GetMyItemsAsync(new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto());

            // Assert
            result.ShouldNotBeNull();
            result.Items.Count.ShouldBeGreaterThan(0);
            result.Items.FirstOrDefault(i => i.Name == itemName).ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task UpdateAsync_Should_Update_Item()
        {
            // Arrange
            var itemName = $"Original_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var createItemDto = new CreateItemDto
            {
                Name = itemName,
                Price = 100m,
                Category = "Cat1"
            };

            var item = await _itemAppService.CreateAsync(createItemDto);

            var newName = $"Updated_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var updateItemDto = new UpdateItemDto
            {
                Name = newName,
                Category = "Cat2",
                Price = 150m
            };

            // Act
            await _itemAppService.UpdateAsync(item.Id, updateItemDto);

            // Assert
            var updated = await _itemAppService.GetAsync(item.Id);
            updated.Name.ShouldBe(newName);
            updated.Price.ShouldBe(150m);
            updated.Category.ShouldBe("Cat2");
        }

        [Fact]
        [UnitOfWork]
        public async Task GetAsync_Should_Return_Item_Details()
        {
            // Arrange
            var itemName = $"Item_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var createItemDto = new CreateItemDto
            {
                Name = itemName,
                Price = 200m,
                Category = "TestCategory"
            };

            var item = await _itemAppService.CreateAsync(createItemDto);

            // Act
            var result = await _itemAppService.GetAsync(item.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(item.Id);
            result.Name.ShouldBe(itemName);
            result.Price.ShouldBe(200m);
        }

        [Fact]
        [UnitOfWork]
        public async Task DeleteAsync_Should_Delete_Draft_Item()
        {
            // Arrange
            var itemName = $"Item_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var createItemDto = new CreateItemDto
            {
                Name = itemName,
                Price = 100m
            };

            var item = await _itemAppService.CreateAsync(createItemDto);

            // Act
            await _itemAppService.DeleteAsync(item.Id);

            // Assert
            var exception = await Should.ThrowAsync<Exception>(
                () => _itemAppService.GetAsync(item.Id)
            );
            exception.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task CreateAsync_Multiple_Items_Should_Have_Unique_Ids()
        {
            // Arrange & Act
            var item1 = await _itemAppService.CreateAsync(new CreateItemDto
            {
                Name = $"Item1_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Price = 100m
            });

            var item2 = await _itemAppService.CreateAsync(new CreateItemDto
            {
                Name = $"Item2_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Price = 200m
            });

            // Assert
            item1.Id.ShouldNotBe(item2.Id);
            item1.Price.ShouldBe(100m);
            item2.Price.ShouldBe(200m);
        }
    }
}
