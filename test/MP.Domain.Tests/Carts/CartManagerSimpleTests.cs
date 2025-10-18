using System;
using System.Linq;
using System.Threading.Tasks;
using MP.Carts;
using MP.Domain.Booths;
using MP.Domain.Carts;
using MP.Domain.Rentals;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Domain.Tests.Carts
{
    public class CartManagerSimpleTests : MPDomainTestBase<MPDomainTestModule>
    {
        private static readonly Guid TestUserId1 = new Guid("00000000-0000-0000-0000-000000000001");
        private static readonly Guid TestUserId2 = new Guid("00000000-0000-0000-0000-000000000002");

        private readonly CartManager _cartManager;
        private readonly ICartRepository _cartRepository;
        private readonly BoothManager _boothManager;
        private readonly IBoothRepository _boothRepository;
        private readonly RentalManager _rentalManager;

        public CartManagerSimpleTests()
        {
            _cartManager = GetRequiredService<CartManager>();
            _cartRepository = GetRequiredService<ICartRepository>();
            _boothManager = GetRequiredService<BoothManager>();
            _boothRepository = GetRequiredService<IBoothRepository>();
            _rentalManager = GetRequiredService<RentalManager>();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetOrCreateActiveCartAsync_Should_Create_Cart()
        {
            // Arrange
            var userId = TestUserId1;

            // Act
            var cart = await _cartManager.GetOrCreateActiveCartAsync(userId);

            // Assert
            cart.ShouldNotBeNull();
            cart.UserId.ShouldBe(userId);
            cart.Status.ShouldBe(CartStatus.Active);
        }

        [Fact]
        [UnitOfWork]
        public async Task GetOrCreateActiveCartAsync_Should_Return_Existing_Cart()
        {
            // Arrange
            var userId = TestUserId2;
            var cart1 = await _cartManager.GetOrCreateActiveCartAsync(userId);

            // Act
            var cart2 = await _cartManager.GetOrCreateActiveCartAsync(userId);

            // Assert
            cart2.Id.ShouldBe(cart1.Id);
        }

        [Fact]
        [UnitOfWork]
        public async Task ValidateCartItemAsync_Should_Throw_When_Date_In_Past()
        {
            // Arrange
            var boothNum = $"PAS{Guid.NewGuid().ToString().Substring(0, 4)}";
            var booth = await _boothManager.CreateAsync(boothNum, 100m);
            await _boothRepository.InsertAsync(booth);
            var pastDate = DateTime.Today.AddDays(-1);
            var futureDate = DateTime.Today.AddDays(10);

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _cartManager.ValidateCartItemAsync(booth.Id, pastDate, futureDate)
            );

            exception.Code.ShouldBe("RENTAL_START_DATE_IN_PAST");
        }

        [Fact]
        [UnitOfWork]
        public async Task ValidateCartItemAsync_Should_Throw_When_Period_Too_Short()
        {
            // Arrange
            var boothNum = $"SRT{Guid.NewGuid().ToString().Substring(0, 4)}";
            var booth = await _boothManager.CreateAsync(boothNum, 100m);
            await _boothRepository.InsertAsync(booth);
            var startDate = DateTime.Today.AddDays(1);
            var endDate = startDate.AddDays(3); // Only 4 days, less than 7

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _cartManager.ValidateCartItemAsync(booth.Id, startDate, endDate)
            );

            exception.Code.ShouldBe("RENTAL_PERIOD_TOO_SHORT");
        }

        [Fact]
        [UnitOfWork]
        public async Task AddItemToCartAsync_Should_Create_Item()
        {
            // Arrange
            var userId = TestUserId1;
            var cart = await _cartManager.GetOrCreateActiveCartAsync(userId);
            var boothNum = $"ADD{Guid.NewGuid().ToString().Substring(0, 4)}";
            var booth = await _boothManager.CreateAsync(boothNum, 100m);
            await _boothRepository.InsertAsync(booth);
            var boothType = Guid.NewGuid(); // Simple booth type ID
            var startDate = DateTime.Today.AddDays(1);
            var endDate = startDate.AddDays(7);

            // Act
            var item = await _cartManager.AddItemToCartAsync(
                cart,
                booth.Id,
                boothType,
                startDate,
                endDate
            );

            // Assert
            item.ShouldNotBeNull();
            item.BoothId.ShouldBe(booth.Id);
            item.StartDate.Date.ShouldBe(startDate.Date);
            item.EndDate.Date.ShouldBe(endDate.Date);
        }

        [Fact]
        [UnitOfWork]
        public async Task UpdateCartItemAsync_Completes_Without_Error()
        {
            // Arrange
            var userId = TestUserId2;
            var cart = await _cartManager.GetOrCreateActiveCartAsync(userId);
            var boothNum = $"UPD{Guid.NewGuid().ToString().Substring(0, 4)}";
            var booth = await _boothManager.CreateAsync(boothNum, 100m);
            await _boothRepository.InsertAsync(booth);
            var boothType = Guid.NewGuid();
            var startDate = DateTime.Today.AddDays(1);
            var endDate = startDate.AddDays(7);

            var item = await _cartManager.AddItemToCartAsync(
                cart,
                booth.Id,
                boothType,
                startDate,
                endDate
            );

            var newStartDate = DateTime.Today.AddDays(5);
            var newEndDate = newStartDate.AddDays(7);

            // Act & Assert - should not throw
            await Should.NotThrowAsync(
                () => _cartManager.UpdateCartItemAsync(
                    cart,
                    item.Id,
                    boothType,
                    newStartDate,
                    newEndDate,
                    "Updated"
                )
            );
        }
    }
}
