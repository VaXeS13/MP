using System;
using System.Linq;
using MP.Carts;
using MP.Domain.Booths;
using MP.Domain.Carts;
using MP.Domain.Promotions;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace MP.Domain.Tests.Carts
{
    public class CartTests : MPDomainTestBase<MPDomainTestModule>
    {
        private readonly Guid _cartId = Guid.NewGuid();
        private readonly Guid _userId = Guid.NewGuid();
        private readonly Guid _boothId = Guid.NewGuid();
        private readonly Guid _boothTypeId = Guid.NewGuid();

        [Fact]
        public void Constructor_Should_Create_Active_Cart()
        {
            // Act
            var cart = new Cart(_cartId, _userId);

            // Assert
            cart.Id.ShouldBe(_cartId);
            cart.UserId.ShouldBe(_userId);
            cart.Status.ShouldBe(CartStatus.Active);
            cart.IsActive().ShouldBeTrue();
        }

        [Fact]
        public void IsEmpty_Should_Return_True_For_New_Cart()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);

            // Act
            var isEmpty = cart.IsEmpty();

            // Assert
            isEmpty.ShouldBeTrue();
        }

        [Fact]
        public void IsEmpty_Should_Return_False_After_Adding_Item()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);
            var item = new CartItem(
                Guid.NewGuid(),
                _cartId,
                _boothId,
                _boothTypeId,
                DateTime.Today.AddDays(7),
                DateTime.Today.AddDays(13),
                100m,
                Currency.PLN
            );

            // Act
            cart.AddItem(item);

            // Assert
            cart.IsEmpty().ShouldBeFalse();
        }

        [Fact]
        public void GetItemCount_Should_Return_Zero_For_New_Cart()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);

            // Act
            var count = cart.GetItemCount();

            // Assert
            count.ShouldBe(0);
        }

        [Fact]
        public void GetItemCount_Should_Return_Correct_Count()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);
            var item1 = new CartItem(Guid.NewGuid(), _cartId, _boothId, _boothTypeId,
                DateTime.Today.AddDays(7), DateTime.Today.AddDays(13), 100m, Currency.PLN);
            var item2 = new CartItem(Guid.NewGuid(), _cartId, Guid.NewGuid(), _boothTypeId,
                DateTime.Today.AddDays(20), DateTime.Today.AddDays(26), 150m, Currency.PLN);

            // Act
            cart.AddItem(item1);
            cart.AddItem(item2);

            // Assert
            cart.GetItemCount().ShouldBe(2);
        }

        [Fact]
        public void AddItem_Should_Add_Item_To_Cart()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);
            var item = new CartItem(Guid.NewGuid(), _cartId, _boothId, _boothTypeId,
                DateTime.Today.AddDays(7), DateTime.Today.AddDays(13), 100m, Currency.PLN);

            // Act
            cart.AddItem(item);

            // Assert
            cart.Items.ShouldContain(item);
            cart.GetItemCount().ShouldBe(1);
        }

        [Fact]
        public void AddItem_Should_Throw_When_Cart_Not_Active()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);
            cart.MarkAsAbandoned();
            var item = new CartItem(Guid.NewGuid(), _cartId, _boothId, _boothTypeId,
                DateTime.Today, DateTime.Today.AddDays(6), 100m, Currency.PLN);

            // Act & Assert
            var exception = Should.Throw<BusinessException>(() => cart.AddItem(item));
            exception.Code.ShouldBe("CART_NOT_ACTIVE");
        }

        [Fact]
        public void RemoveItem_Should_Remove_Item_From_Cart()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);
            var itemId = Guid.NewGuid();
            var item = new CartItem(itemId, _cartId, _boothId, _boothTypeId,
                DateTime.Today.AddDays(7), DateTime.Today.AddDays(13), 100m, Currency.PLN);
            cart.AddItem(item);

            // Act
            cart.RemoveItem(itemId);

            // Assert
            cart.Items.ShouldNotContain(item);
            cart.IsEmpty().ShouldBeTrue();
        }

        [Fact]
        public void RemoveItem_Should_Throw_When_Item_Not_Found()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);

            // Act & Assert
            var exception = Should.Throw<BusinessException>(
                () => cart.RemoveItem(Guid.NewGuid())
            );
            exception.Code.ShouldBe("CART_ITEM_NOT_FOUND");
        }

        [Fact]
        public void Clear_Should_Remove_All_Items()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);
            cart.AddItem(new CartItem(Guid.NewGuid(), _cartId, _boothId, _boothTypeId,
                DateTime.Today.AddDays(7), DateTime.Today.AddDays(13), 100m, Currency.PLN));
            cart.AddItem(new CartItem(Guid.NewGuid(), _cartId, Guid.NewGuid(), _boothTypeId,
                DateTime.Today.AddDays(20), DateTime.Today.AddDays(26), 150m, Currency.PLN));

            // Act
            cart.Clear();

            // Assert
            cart.IsEmpty().ShouldBeTrue();
            cart.GetItemCount().ShouldBe(0);
        }

        [Fact]
        public void Clear_Should_Throw_When_Cart_Not_Active()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);
            cart.MarkAsAbandoned();

            // Act & Assert
            var exception = Should.Throw<BusinessException>(() => cart.Clear());
            exception.Code.ShouldBe("CART_NOT_ACTIVE");
        }

        [Fact]
        public void MarkAsCheckedOut_Should_Change_Status()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);
            cart.AddItem(new CartItem(Guid.NewGuid(), _cartId, _boothId, _boothTypeId,
                DateTime.Today.AddDays(7), DateTime.Today.AddDays(13), 100m, Currency.PLN));

            // Act
            cart.MarkAsCheckedOut();

            // Assert
            cart.Status.ShouldBe(CartStatus.CheckedOut);
            cart.IsActive().ShouldBeFalse();
        }

        [Fact]
        public void MarkAsCheckedOut_Should_Throw_When_Cart_Empty()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);

            // Act & Assert
            var exception = Should.Throw<BusinessException>(() => cart.MarkAsCheckedOut());
            exception.Code.ShouldBe("CART_IS_EMPTY");
        }

        [Fact]
        public void MarkAsAbandoned_Should_Change_Status()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);

            // Act
            cart.MarkAsAbandoned();

            // Assert
            cart.Status.ShouldBe(CartStatus.Abandoned);
        }

        [Fact]
        public void MarkAsAbandoned_Should_Throw_When_Already_CheckedOut()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);
            cart.AddItem(new CartItem(Guid.NewGuid(), _cartId, _boothId, _boothTypeId,
                DateTime.Today.AddDays(7), DateTime.Today.AddDays(13), 100m, Currency.PLN));
            cart.MarkAsCheckedOut();

            // Act & Assert
            var exception = Should.Throw<BusinessException>(() => cart.MarkAsAbandoned());
            exception.Code.ShouldBe("CANNOT_ABANDON_CHECKED_OUT_CART");
        }

        [Fact]
        public void GetTotalAmount_Should_Sum_All_Items()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);
            // Item 1: 100 per day * 7 days = 700
            cart.AddItem(new CartItem(Guid.NewGuid(), _cartId, _boothId, _boothTypeId,
                DateTime.Today.AddDays(7), DateTime.Today.AddDays(13), 100m, Currency.PLN));
            // Item 2: 150 per day * 7 days = 1050
            cart.AddItem(new CartItem(Guid.NewGuid(), _cartId, Guid.NewGuid(), _boothTypeId,
                DateTime.Today.AddDays(20), DateTime.Today.AddDays(26), 150m, Currency.PLN));

            // Act
            var total = cart.GetTotalAmount();

            // Assert
            total.ShouldBe(1750m);
        }

        [Fact]
        public void GetTotalAmount_Should_Return_Zero_For_Empty_Cart()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);

            // Act
            var total = cart.GetTotalAmount();

            // Assert
            total.ShouldBe(0);
        }

        [Fact]
        public void GetTotalDays_Should_Sum_All_Days()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);
            // Item 1: 7 days
            cart.AddItem(new CartItem(Guid.NewGuid(), _cartId, _boothId, _boothTypeId,
                DateTime.Today.AddDays(7), DateTime.Today.AddDays(13), 100m, Currency.PLN));
            // Item 2: 7 days
            cart.AddItem(new CartItem(Guid.NewGuid(), _cartId, Guid.NewGuid(), _boothTypeId,
                DateTime.Today.AddDays(20), DateTime.Today.AddDays(26), 150m, Currency.PLN));

            // Act
            var totalDays = cart.GetTotalDays();

            // Assert
            totalDays.ShouldBe(14);
        }

        [Fact]
        public void HasBoothInCart_Should_Return_True_When_Booth_Present()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);
            cart.AddItem(new CartItem(Guid.NewGuid(), _cartId, _boothId, _boothTypeId,
                DateTime.Today.AddDays(7), DateTime.Today.AddDays(13), 100m, Currency.PLN));

            // Act
            var hasItem = cart.HasBoothInCart(_boothId);

            // Assert
            hasItem.ShouldBeTrue();
        }

        [Fact]
        public void HasBoothInCart_Should_Return_False_When_Booth_Not_Present()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);

            // Act
            var hasItem = cart.HasBoothInCart(_boothId);

            // Assert
            hasItem.ShouldBeFalse();
        }

        [Fact]
        public void HasOverlappingBooking_Should_Return_True_For_Overlapping_Dates()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);
            var startDate = DateTime.Today.AddDays(10);
            var endDate = DateTime.Today.AddDays(16);
            cart.AddItem(new CartItem(Guid.NewGuid(), _cartId, _boothId, _boothTypeId,
                DateTime.Today.AddDays(7), DateTime.Today.AddDays(13), 100m, Currency.PLN));

            // Act
            var hasOverlap = cart.HasOverlappingBooking(_boothId, startDate, endDate);

            // Assert
            hasOverlap.ShouldBeTrue();
        }

        [Fact]
        public void HasOverlappingBooking_Should_Return_False_For_Non_Overlapping_Dates()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);
            cart.AddItem(new CartItem(Guid.NewGuid(), _cartId, _boothId, _boothTypeId,
                DateTime.Today.AddDays(7), DateTime.Today.AddDays(13), 100m, Currency.PLN));

            // Act
            var hasOverlap = cart.HasOverlappingBooking(_boothId, DateTime.Today.AddDays(20), DateTime.Today.AddDays(26));

            // Assert
            hasOverlap.ShouldBeFalse();
        }

        [Fact]
        public void IsActive_Should_Return_True_For_Active_Cart()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);

            // Act & Assert
            cart.IsActive().ShouldBeTrue();
        }

        [Fact]
        public void IsActive_Should_Return_False_For_Abandoned_Cart()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);
            cart.MarkAsAbandoned();

            // Act & Assert
            cart.IsActive().ShouldBeFalse();
        }

        [Fact]
        public void IsActive_Should_Return_False_For_CheckedOut_Cart()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);
            cart.AddItem(new CartItem(Guid.NewGuid(), _cartId, _boothId, _boothTypeId,
                DateTime.Today.AddDays(7), DateTime.Today.AddDays(13), 100m, Currency.PLN));
            cart.MarkAsCheckedOut();

            // Act & Assert
            cart.IsActive().ShouldBeFalse();
        }

        [Fact]
        public void SetExtensionTimeout_Should_Set_Timeout()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);
            var timeout = DateTime.Now.AddHours(1);

            // Act
            cart.SetExtensionTimeout(timeout);

            // Assert
            cart.ExtensionTimeoutAt.ShouldBe(timeout);
        }

        [Fact]
        public void SetExtensionTimeout_Should_Allow_Null_Timeout()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);
            cart.SetExtensionTimeout(DateTime.Now.AddHours(1));

            // Act
            cart.SetExtensionTimeout(null);

            // Assert
            cart.ExtensionTimeoutAt.ShouldBeNull();
        }

        [Fact]
        public void Cart_Should_Have_Default_Currency_For_TenantId()
        {
            // Arrange & Act
            var cart = new Cart(_cartId, _userId, null);

            // Assert
            cart.TenantId.ShouldBeNull();
        }

        [Fact]
        public void Cart_Should_Support_Multi_Tenancy()
        {
            // Arrange
            var tenantId = Guid.NewGuid();

            // Act
            var cart = new Cart(_cartId, _userId, tenantId);

            // Assert
            cart.TenantId.ShouldBe(tenantId);
        }

        [Fact]
        public void RemoveItem_Should_Throw_When_Cart_Not_Active()
        {
            // Arrange
            var cart = new Cart(_cartId, _userId);
            var itemId = Guid.NewGuid();
            cart.AddItem(new CartItem(itemId, _cartId, _boothId, _boothTypeId,
                DateTime.Today.AddDays(7), DateTime.Today.AddDays(13), 100m, Currency.PLN));
            cart.MarkAsAbandoned();

            // Act & Assert
            var exception = Should.Throw<BusinessException>(() => cart.RemoveItem(itemId));
            exception.Code.ShouldBe("CART_NOT_ACTIVE");
        }
    }
}
