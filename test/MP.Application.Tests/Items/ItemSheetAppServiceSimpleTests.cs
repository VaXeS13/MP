using System;
using System.Linq;
using System.Threading.Tasks;
using MP.Items;
using Shouldly;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Application.Tests.Items
{
    public class ItemSheetAppServiceSimpleTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly IItemSheetAppService _itemSheetAppService;
        private readonly IItemAppService _itemAppService;

        public ItemSheetAppServiceSimpleTests()
        {
            _itemSheetAppService = GetRequiredService<IItemSheetAppService>();
            _itemAppService = GetRequiredService<IItemAppService>();
        }

        [Fact]
        [UnitOfWork]
        public async Task CreateAsync_Should_Create_ItemSheet()
        {
            // Arrange
            var createDto = new CreateItemSheetDto();

            // Act
            var result = await _itemSheetAppService.CreateAsync(createDto);

            // Assert
            result.ShouldNotBeNull();
            result.Items.ShouldBeEmpty();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetAsync_Should_Return_ItemSheet()
        {
            // Arrange
            var created = await _itemSheetAppService.CreateAsync(new CreateItemSheetDto());

            // Act
            var result = await _itemSheetAppService.GetAsync(created.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(created.Id);
        }

        [Fact]
        [UnitOfWork]
        public async Task GetMyItemSheetsAsync_Should_Return_User_Sheets()
        {
            // Arrange
            var created = await _itemSheetAppService.CreateAsync(new CreateItemSheetDto());

            // Act
            var result = await _itemSheetAppService.GetMyItemSheetsAsync(
                new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto()
            );

            // Assert
            result.ShouldNotBeNull();
            result.Items.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        [UnitOfWork]
        public async Task AddItemToSheetAsync_Should_Add_Item()
        {
            // Arrange
            var sheet = await _itemSheetAppService.CreateAsync(new CreateItemSheetDto());
            var item = await _itemAppService.CreateAsync(new CreateItemDto
            {
                Name = $"Item_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Price = 100m,
                Category = "TestCategory"
            });

            var addDto = new AddItemToSheetDto
            {
                ItemId = item.Id,
                CommissionPercentage = 10m
            };

            // Act
            var result = await _itemSheetAppService.AddItemToSheetAsync(sheet.Id, addDto);

            // Assert
            result.ShouldNotBeNull();
            result.Items.ShouldNotBeEmpty();
            result.Items.First().ItemId.ShouldBe(item.Id);
        }

        [Fact]
        [UnitOfWork]
        public async Task RemoveItemFromSheetAsync_Should_Remove_Item()
        {
            // Arrange
            var sheet = await _itemSheetAppService.CreateAsync(new CreateItemSheetDto());
            var item = await _itemAppService.CreateAsync(new CreateItemDto
            {
                Name = $"Item_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Price = 50m,
                Category = "Category"
            });

            var addDto = new AddItemToSheetDto
            {
                ItemId = item.Id,
                CommissionPercentage = 5m
            };

            await _itemSheetAppService.AddItemToSheetAsync(sheet.Id, addDto);

            // Act
            var result = await _itemSheetAppService.RemoveItemFromSheetAsync(sheet.Id, item.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Items.ShouldBeEmpty();
        }

        [Fact]
        [UnitOfWork]
        public async Task GenerateBarcodesAsync_Should_Generate_Barcodes()
        {
            // Arrange
            var sheet = await _itemSheetAppService.CreateAsync(new CreateItemSheetDto());
            var item = await _itemAppService.CreateAsync(new CreateItemDto
            {
                Name = $"Item_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Price = 75m,
                Category = "TestCategory"
            });

            var addDto = new AddItemToSheetDto
            {
                ItemId = item.Id
            };

            await _itemSheetAppService.AddItemToSheetAsync(sheet.Id, addDto);

            // Act
            var result = await _itemSheetAppService.GenerateBarcodesAsync(sheet.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Items.ShouldNotBeEmpty();
        }

        [Fact]
        [UnitOfWork]
        public async Task DeleteAsync_Should_Delete_ItemSheet()
        {
            // Arrange
            var created = await _itemSheetAppService.CreateAsync(new CreateItemSheetDto());

            // Act
            await _itemSheetAppService.DeleteAsync(created.Id);

            // Assert
            var exception = await Should.ThrowAsync<Exception>(
                () => _itemSheetAppService.GetAsync(created.Id)
            );
            exception.ShouldNotBeNull();
        }
    }
}
