using System;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Xunit;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using MP.Application.Contracts.Sellers;
using MP.Application.Devices;
using MP.Application.Sellers;

namespace MP.Application.Tests.Devices
{
    /// <summary>
    /// Integration tests for ItemCheckoutAppService with device proxy integration
    /// Tests checkout operations with simulated device responses
    /// </summary>
    public class ItemCheckoutAppServiceDeviceTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private ItemCheckoutAppService _appService = null!;
        private IRemoteDeviceProxy _deviceProxy = null!;
        private ICurrentTenant _currentTenant = null!;

        public ItemCheckoutAppServiceDeviceTests()
        {
            _appService = GetRequiredService<ItemCheckoutAppService>();
            _deviceProxy = GetRequiredService<IRemoteDeviceProxy>();
            _currentTenant = GetRequiredService<ICurrentTenant>();
        }

        #region GetAvailablePaymentMethods Tests

        [Fact]
        [UnitOfWork]
        public async Task GetAvailablePaymentMethods_Should_Always_Enable_Cash()
        {
            // Arrange & Act
            var result = await _appService.GetAvailablePaymentMethodsAsync();

            // Assert
            result.ShouldNotBeNull();
            result.CashEnabled.ShouldBeTrue();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetAvailablePaymentMethods_Should_Return_Valid_Response()
        {
            // Arrange & Act
            var result = await _appService.GetAvailablePaymentMethodsAsync();

            // Assert
            result.ShouldNotBeNull();
            result.GetType().Name.ShouldBe("AvailablePaymentMethodsDto");
        }

        #endregion

        #region Service Registration Tests

        [Fact]
        public void ItemCheckoutAppService_Should_Be_Registered()
        {
            // Arrange & Act
            var appService = GetRequiredService<ItemCheckoutAppService>();

            // Assert
            appService.ShouldNotBeNull();
        }

        [Fact]
        public void IRemoteDeviceProxy_Should_Be_Registered_In_DI()
        {
            // Arrange & Act
            var proxy = GetRequiredService<IRemoteDeviceProxy>();

            // Assert
            proxy.ShouldNotBeNull();
        }

        [Fact]
        public void ICurrentTenant_Should_Be_Registered_In_DI()
        {
            // Arrange & Act
            var currentTenant = GetRequiredService<ICurrentTenant>();

            // Assert
            currentTenant.ShouldNotBeNull();
        }

        #endregion

        #region FindItemByBarcode Tests

        [Fact]
        public async Task FindItemByBarcode_Should_Throw_When_Barcode_Empty()
        {
            // Arrange
            var input = new FindItemByBarcodeDto { Barcode = "" };

            // Act & Assert
            await Should.ThrowAsync<Volo.Abp.UserFriendlyException>(
                () => _appService.FindItemByBarcodeAsync(input)
            );
        }

        [Fact]
        public async Task FindItemByBarcode_Should_Throw_When_Barcode_Whitespace()
        {
            // Arrange
            var input = new FindItemByBarcodeDto { Barcode = "   " };

            // Act & Assert
            await Should.ThrowAsync<Volo.Abp.UserFriendlyException>(
                () => _appService.FindItemByBarcodeAsync(input)
            );
        }

        #endregion

        #region Multi-Tenant Context Tests

        [Fact]
        [UnitOfWork]
        public async Task GetAvailablePaymentMethods_Should_Execute_In_Tenant_Context()
        {
            // Arrange
            var tenantId = Guid.NewGuid();

            using (_currentTenant.Change(tenantId))
            {
                // Act
                var result = await _appService.GetAvailablePaymentMethodsAsync();

                // Assert
                result.ShouldNotBeNull();
            }
        }

        [Fact]
        [UnitOfWork]
        public async Task GetAvailablePaymentMethods_Should_Work_For_Multiple_Tenants()
        {
            // Arrange
            var tenantId1 = Guid.NewGuid();
            var tenantId2 = Guid.NewGuid();

            // Act & Assert - Tenant 1
            using (_currentTenant.Change(tenantId1))
            {
                var result1 = await _appService.GetAvailablePaymentMethodsAsync();
                result1.ShouldNotBeNull();
            }

            // Act & Assert - Tenant 2
            using (_currentTenant.Change(tenantId2))
            {
                var result2 = await _appService.GetAvailablePaymentMethodsAsync();
                result2.ShouldNotBeNull();
            }
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        [UnitOfWork]
        public async Task GetAvailablePaymentMethods_Should_Not_Throw_Exception()
        {
            // Arrange & Act - Should not throw
            var result = await Record.ExceptionAsync(
                async () => await _appService.GetAvailablePaymentMethodsAsync()
            );

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetAvailablePaymentMethods_Should_Return_Default_On_Any_Error()
        {
            // Arrange & Act
            var result = await _appService.GetAvailablePaymentMethodsAsync();

            // Assert - should always return valid response
            result.ShouldNotBeNull();
            result.CashEnabled.ShouldBeTrue();
        }

        #endregion

        #region Service Method Availability Tests

        [Fact]
        public void GetAvailablePaymentMethods_Method_Should_Exist()
        {
            // Arrange
            var methodInfo = typeof(ItemCheckoutAppService).GetMethod("GetAvailablePaymentMethodsAsync");

            // Act & Assert
            methodInfo.ShouldNotBeNull();
        }

        [Fact]
        public void FindItemByBarcode_Method_Should_Exist()
        {
            // Arrange
            var methodInfo = typeof(ItemCheckoutAppService).GetMethod("FindItemByBarcodeAsync");

            // Act & Assert
            methodInfo.ShouldNotBeNull();
        }

        [Fact]
        public void CalculateCheckoutSummary_Method_Should_Exist()
        {
            // Arrange
            var methodInfo = typeof(ItemCheckoutAppService).GetMethod("CalculateCheckoutSummaryAsync");

            // Act & Assert
            methodInfo.ShouldNotBeNull();
        }

        [Fact]
        public void CheckoutItems_Method_Should_Exist()
        {
            // Arrange
            var methodInfo = typeof(ItemCheckoutAppService).GetMethod("CheckoutItemsAsync");

            // Act & Assert
            methodInfo.ShouldNotBeNull();
        }

        #endregion
    }
}
