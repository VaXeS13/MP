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

    }
}
