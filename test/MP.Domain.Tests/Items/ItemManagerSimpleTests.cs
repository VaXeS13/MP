using System;
using System.Threading.Tasks;
using MP.Domain.Booths;
using MP.Domain.Items;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Domain.Tests.Items
{
    public class ItemManagerSimpleTests : MPDomainTestBase<MPDomainTestModule>
    {
        private static readonly Guid TestUserId1 = new Guid("00000000-0000-0000-0000-000000000001");
        private static readonly Guid TestUserId2 = new Guid("00000000-0000-0000-0000-000000000002");

        private readonly ItemManager _itemManager;
        private readonly IItemRepository _itemRepository;
        private readonly IItemSheetRepository _itemSheetRepository;

        public ItemManagerSimpleTests()
        {
            _itemManager = GetRequiredService<ItemManager>();
            _itemRepository = GetRequiredService<IItemRepository>();
            _itemSheetRepository = GetRequiredService<IItemSheetRepository>();
        }

        [Fact]
        [UnitOfWork]
        public async Task CreateAsync_Should_Create_Item()
        {
            // Arrange
            var itemName = $"Item_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var userId = TestUserId1;

            // Act
            var item = await _itemManager.CreateAsync(
                userId,
                itemName,
                100m,
                Currency.PLN,
                "Category1"
            );

            // Assert
            item.ShouldNotBeNull();
            item.UserId.ShouldBe(userId);
            item.Name.ShouldBe(itemName);
            item.Price.ShouldBe(100m);
            item.Category.ShouldBe("Category1");
            item.Status.ShouldBe(ItemStatus.Draft);
        }

        [Fact]
        [UnitOfWork]
        public async Task CreateSheetAsync_Should_Create_ItemSheet()
        {
            // Arrange
            var userId = TestUserId1;

            // Act
            var sheet = await _itemManager.CreateSheetAsync(userId);

            // Assert
            sheet.ShouldNotBeNull();
            sheet.UserId.ShouldBe(userId);
            sheet.Items.Count.ShouldBe(0);
        }

        [Fact]
        [UnitOfWork]
        public async Task AddItemToSheetAsync_Should_Add_Item_To_Sheet()
        {
            // Arrange
            var userId = TestUserId1;
            var itemName = $"Item_{Guid.NewGuid().ToString().Substring(0, 8)}";

            var item = await _itemManager.CreateAsync(userId, itemName, 100m, Currency.PLN);
            var sheet = await _itemManager.CreateSheetAsync(userId);

            // Act
            await _itemManager.AddItemToSheetAsync(sheet, item, 10m);

            // Assert
            sheet.Items.Count.ShouldBe(1);
            item.Status.ShouldBe(ItemStatus.InSheet);
        }

        [Fact]
        [UnitOfWork]
        public async Task AddItemToSheetAsync_Should_Throw_When_Item_Not_Draft()
        {
            // Arrange
            var userId = TestUserId1;
            var itemName = $"Item_{Guid.NewGuid().ToString().Substring(0, 8)}";

            var item = await _itemManager.CreateAsync(userId, itemName, 100m, Currency.PLN);
            var sheet = await _itemManager.CreateSheetAsync(userId);

            // Add item first
            await _itemManager.AddItemToSheetAsync(sheet, item);

            var sheet2 = await _itemManager.CreateSheetAsync(userId);

            // Act & Assert - should throw because item is no longer Draft
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _itemManager.AddItemToSheetAsync(sheet2, item)
            );

            exception.Code.ShouldBe("ONLY_DRAFT_ITEMS_CAN_BE_ADDED_TO_SHEET");
        }

        [Fact]
        [UnitOfWork]
        public async Task AddItemToSheetAsync_Should_Throw_When_Users_Mismatch()
        {
            // Arrange
            var user1 = TestUserId1;
            var user2 = TestUserId2;
            var itemName = $"Item_{Guid.NewGuid().ToString().Substring(0, 8)}";

            var item = await _itemManager.CreateAsync(user1, itemName, 100m, Currency.PLN);
            var sheet = await _itemManager.CreateSheetAsync(user2);

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _itemManager.AddItemToSheetAsync(sheet, item)
            );

            exception.Code.ShouldBe("ITEM_AND_SHEET_MUST_BELONG_TO_SAME_USER");
        }

        [Fact]
        [UnitOfWork]
        public async Task RemoveItemFromSheetAsync_Should_Remove_Item()
        {
            // Arrange
            var userId = TestUserId1;
            var itemName = $"Item_{Guid.NewGuid().ToString().Substring(0, 8)}";

            var item = await _itemManager.CreateAsync(userId, itemName, 100m, Currency.PLN);
            var sheet = await _itemManager.CreateSheetAsync(userId);

            await _itemManager.AddItemToSheetAsync(sheet, item);
            sheet.Items.Count.ShouldBe(1);

            // Act
            await _itemManager.RemoveItemFromSheetAsync(sheet, item);

            // Assert
            sheet.Items.Count.ShouldBe(0);
            item.Status.ShouldBe(ItemStatus.Draft);
        }
    }
}
