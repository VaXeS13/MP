using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MP.Carts;
using MP.Domain.Booths;
using MP.Domain.BoothTypes;
using MP.Domain.Rentals;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Application.Tests.Carts
{
    public class CartAppServiceTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly ICartAppService _cartAppService;
        private readonly IBoothRepository _boothRepository;
        private readonly IBoothTypeRepository _boothTypeRepository;
        private readonly IRepository<IdentityUser, Guid> _userRepository;

        public CartAppServiceTests()
        {
            _cartAppService = GetRequiredService<ICartAppService>();
            _boothRepository = GetRequiredService<IBoothRepository>();
            _boothTypeRepository = GetRequiredService<IBoothTypeRepository>();
            _userRepository = GetRequiredService<IRepository<IdentityUser, Guid>>();
        }

        private async Task CleanupCartAsync()
        {
            // Clean up any carts for the current test user before each test
            await _cartAppService.ClearCartAsync();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetMyCartAsync_Should_Return_Empty_Cart_For_New_User()
        {
            // Arrange
            await CleanupCartAsync();

            // Act
            var cart = await _cartAppService.GetMyCartAsync();

            // Assert
            cart.ShouldNotBeNull();
            cart.Items.ShouldBeEmpty();
            cart.ItemCount.ShouldBe(0);
            cart.TotalAmount.ShouldBe(0);
        }

        [Fact]
        [UnitOfWork]
        public async Task GetMyCartAsync_Should_Return_Existing_Cart()
        {
            // Arrange - Add an item to cart first
            await CleanupCartAsync();
            var booth = await CreateTestBoothAsync();
            var boothType = await CreateTestBoothTypeAsync();

            var addToCartDto = new AddToCartDto
            {
                BoothId = booth.Id,
                BoothTypeId = boothType.Id,
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(13)
            };

            await _cartAppService.AddItemAsync(addToCartDto);

            // Act
            var cart = await _cartAppService.GetMyCartAsync();

            // Assert
            cart.ShouldNotBeNull();
            cart.Items.ShouldNotBeEmpty();
            cart.ItemCount.ShouldBe(1);
        }

        [Fact]
        [UnitOfWork]
        public async Task AddItemAsync_Should_Add_Booth_To_Cart()
        {
            // Arrange
            await CleanupCartAsync();
            var booth = await CreateTestBoothAsync();
            var boothType = await CreateTestBoothTypeAsync();

            var addToCartDto = new AddToCartDto
            {
                BoothId = booth.Id,
                BoothTypeId = boothType.Id,
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(13),
                Notes = "Test notes"
            };

            // Act
            var result = await _cartAppService.AddItemAsync(addToCartDto);

            // Assert
            result.ShouldNotBeNull();
            result.Items.ShouldNotBeEmpty();
            result.ItemCount.ShouldBe(1);
            result.Items.First().BoothId.ShouldBe(booth.Id);
        }

        [Fact]
        [UnitOfWork]
        public async Task AddItemAsync_Should_Calculate_Correct_Total_Amount()
        {
            // Arrange
            await CleanupCartAsync();
            var boothPrice = 100m;
            var booth = await CreateTestBoothAsync(price: boothPrice);
            var boothType = await CreateTestBoothTypeAsync();

            var addToCartDto = new AddToCartDto
            {
                BoothId = booth.Id,
                BoothTypeId = boothType.Id,
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(13) // 7 days
            };

            // Act
            var result = await _cartAppService.AddItemAsync(addToCartDto);

            // Assert
            var expectedAmount = boothPrice * 7; // 7 days
            result.TotalAmount.ShouldBe(expectedAmount);
        }

        [Fact]
        [UnitOfWork]
        public async Task AddItemAsync_Should_Throw_When_Booth_Already_In_Cart()
        {
            // Arrange
            await CleanupCartAsync();
            var booth = await CreateTestBoothAsync(price: 100m);
            var boothType = await CreateTestBoothTypeAsync();

            var addToCartDto = new AddToCartDto
            {
                BoothId = booth.Id,
                BoothTypeId = boothType.Id,
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(13)
            };

            // Add first time - should succeed
            await _cartAppService.AddItemAsync(addToCartDto);

            // Act & Assert - Add same booth again - should throw
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _cartAppService.AddItemAsync(addToCartDto)
            );

            exception.Code.ShouldBe("CART_BOOTH_ALREADY_ADDED_WITH_OVERLAPPING_DATES");
        }

        [Fact]
        [UnitOfWork]
        public async Task RemoveItemAsync_Should_Remove_Item_From_Cart()
        {
            // Arrange
            await CleanupCartAsync();
            var booth = await CreateTestBoothAsync();
            var boothType = await CreateTestBoothTypeAsync();

            var addToCartDto = new AddToCartDto
            {
                BoothId = booth.Id,
                BoothTypeId = boothType.Id,
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(13)
            };

            var cart = await _cartAppService.AddItemAsync(addToCartDto);
            var itemId = cart.Items.First().Id;

            // Act
            var result = await _cartAppService.RemoveItemAsync(itemId);

            // Assert
            result.Items.ShouldBeEmpty();
            result.ItemCount.ShouldBe(0);
        }

        [Fact]
        [UnitOfWork]
        public async Task RemoveItemAsync_Should_Throw_When_Item_Not_In_Cart()
        {
            // Arrange
            await CleanupCartAsync();
            var fakeItemId = Guid.NewGuid();

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _cartAppService.RemoveItemAsync(fakeItemId)
            );

            exception.Code.ShouldBe("CART_ITEM_NOT_FOUND");
        }

        [Fact]
        [UnitOfWork]
        public async Task UpdateItemAsync_Should_Update_Cart_Item_Dates()
        {
            // Arrange
            await CleanupCartAsync();
            var booth = await CreateTestBoothAsync();
            var boothType = await CreateTestBoothTypeAsync();

            var addToCartDto = new AddToCartDto
            {
                BoothId = booth.Id,
                BoothTypeId = boothType.Id,
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(13)
            };

            var cart = await _cartAppService.AddItemAsync(addToCartDto);
            var itemId = cart.Items.First().Id;

            var updateDto = new UpdateCartItemDto
            {
                StartDate = DateTime.Today.AddDays(10),
                EndDate = DateTime.Today.AddDays(15) // 6 days total
            };

            // Act
            var result = await _cartAppService.UpdateItemAsync(itemId, updateDto);

            // Assert
            var updatedItem = result.Items.First();
            updatedItem.StartDate.ShouldBe(updateDto.StartDate);
            updatedItem.EndDate.ShouldBe(updateDto.EndDate);
        }

        [Fact]
        [UnitOfWork]
        public async Task UpdateItemAsync_Should_Recalculate_Total_Amount_After_Update()
        {
            // Arrange
            await CleanupCartAsync();
            var boothPrice = 100m;
            var booth = await CreateTestBoothAsync(price: boothPrice);
            var boothType = await CreateTestBoothTypeAsync();

            var addToCartDto = new AddToCartDto
            {
                BoothId = booth.Id,
                BoothTypeId = boothType.Id,
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(13) // 7 days
            };

            var cart = await _cartAppService.AddItemAsync(addToCartDto);
            var initialAmount = cart.TotalAmount;
            var itemId = cart.Items.First().Id;

            var updateDto = new UpdateCartItemDto
            {
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(16) // 10 days
            };

            // Act
            var result = await _cartAppService.UpdateItemAsync(itemId, updateDto);

            // Assert
            var expectedAmount = boothPrice * 10; // 10 days
            result.TotalAmount.ShouldBe(expectedAmount);
            result.TotalAmount.ShouldBeGreaterThan(initialAmount);
        }

        [Fact]
        [UnitOfWork]
        public async Task ClearCartAsync_Should_Remove_All_Items()
        {
            // Arrange
            await CleanupCartAsync();
            var booth1 = await CreateTestBoothAsync(price: 100m);
            var booth2 = await CreateTestBoothAsync("BOOTH02", 50m);
            var boothType = await CreateTestBoothTypeAsync();

            var addToCartDto1 = new AddToCartDto
            {
                BoothId = booth1.Id,
                BoothTypeId = boothType.Id,
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(13)
            };

            var addToCartDto2 = new AddToCartDto
            {
                BoothId = booth2.Id,
                BoothTypeId = boothType.Id,
                StartDate = DateTime.Today.AddDays(20),
                EndDate = DateTime.Today.AddDays(26)
            };

            await _cartAppService.AddItemAsync(addToCartDto1);
            await _cartAppService.AddItemAsync(addToCartDto2);

            // Act
            var result = await _cartAppService.ClearCartAsync();

            // Assert
            result.Items.ShouldBeEmpty();
            result.ItemCount.ShouldBe(0);
            result.TotalAmount.ShouldBe(0);
        }

        [Fact]
        [UnitOfWork]
        public async Task CheckoutAsync_Should_Create_Rentals_And_Clear_Cart()
        {
            // Arrange
            await CleanupCartAsync();
            var booth = await CreateTestBoothAsync();
            var boothType = await CreateTestBoothTypeAsync();

            var addToCartDto = new AddToCartDto
            {
                BoothId = booth.Id,
                BoothTypeId = boothType.Id,
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(13)
            };

            await _cartAppService.AddItemAsync(addToCartDto);

            var checkoutDto = new CheckoutCartDto
            {
                PaymentProviderId = "p24",
                PaymentMethodId = null
            };

            // Act
            var result = await _cartAppService.CheckoutAsync(checkoutDto);

            // Assert
            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
            result.RentalIds.Count.ShouldBeGreaterThan(0);

            // Verify cart is cleared
            var cart = await _cartAppService.GetMyCartAsync();
            cart.Items.ShouldBeEmpty();
        }

        [Fact]
        [UnitOfWork]
        public async Task CheckoutAsync_Should_Throw_When_Cart_Empty()
        {
            // Arrange
            await CleanupCartAsync();
            var checkoutDto = new CheckoutCartDto
            {
                PaymentProviderId = "p24",
                PaymentMethodId = null
            };

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _cartAppService.CheckoutAsync(checkoutDto)
            );

            exception.Code.ShouldBe("CART_IS_EMPTY");
        }

        // Helper methods
        private async Task<MP.Domain.Booths.Booth> CreateTestBoothAsync(string number = "TEST-01", decimal price = 100m)
        {
            var booth = new MP.Domain.Booths.Booth(Guid.NewGuid(), number, price);
            await _boothRepository.InsertAsync(booth);
            return booth;
        }

        private async Task<BoothType> CreateTestBoothTypeAsync(string name = "Standard", string description = "Test booth type", decimal commission = 10m)
        {
            var boothType = new BoothType(Guid.NewGuid(), name, description, commission);
            await _boothTypeRepository.InsertAsync(boothType);
            return boothType;
        }
    }
}
