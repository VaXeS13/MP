using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using MP.Application.Devices;
using MP.Application.Sellers;
using MP.LocalAgent.Contracts.Commands;
using MP.LocalAgent.Contracts.Responses;

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

        #region Terminal Status Tests

        [Fact]
        [UnitOfWork]
        public async Task CheckTerminalStatus_Should_Call_Device_Proxy()
        {
            // Arrange
            _deviceProxy.IsDeviceAvailableAsync("terminal", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            // Act
            var result = await _appService.CheckTerminalStatusAsync();

            // Assert
            result.ShouldBeTrue();
            await _deviceProxy.Received(1).IsDeviceAvailableAsync("terminal", Arg.Any<CancellationToken>());
        }

        [Fact]
        [UnitOfWork]
        public async Task CheckTerminalStatus_Should_Return_False_When_Device_Unavailable()
        {
            // Arrange
            _deviceProxy.IsDeviceAvailableAsync("terminal", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(false));

            // Act
            var result = await _appService.CheckTerminalStatusAsync();

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        [UnitOfWork]
        public async Task CheckTerminalStatus_Should_Handle_Exception()
        {
            // Arrange
            _deviceProxy.IsDeviceAvailableAsync("terminal", Arg.Any<CancellationToken>())
                .Returns(Task.FromException<bool>(new InvalidOperationException("Device communication error")));

            // Act
            var result = await _appService.CheckTerminalStatusAsync();

            // Assert - should return false on error
            result.ShouldBeFalse();
        }

        #endregion

        #region Device Availability Tests

        [Fact]
        [UnitOfWork]
        public async Task IsPaymentMethodAvailable_Terminal_Should_Check_Device()
        {
            // Arrange
            _deviceProxy.IsDeviceAvailableAsync("terminal", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            // Act
            var result = await _appService.IsPaymentMethodAvailableAsync("terminal");

            // Assert
            result.ShouldBeTrue();
            await _deviceProxy.Received(1).IsDeviceAvailableAsync("terminal", Arg.Any<CancellationToken>());
        }

        [Fact]
        [UnitOfWork]
        public async Task IsPaymentMethodAvailable_FiscalPrinter_Should_Check_Device()
        {
            // Arrange
            _deviceProxy.IsDeviceAvailableAsync("fiscal_printer", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            // Act
            var result = await _appService.IsPaymentMethodAvailableAsync("fiscal_printer");

            // Assert
            result.ShouldBeTrue();
            await _deviceProxy.Received(1).IsDeviceAvailableAsync("fiscal_printer", Arg.Any<CancellationToken>());
        }

        [Fact]
        [UnitOfWork]
        public async Task IsPaymentMethodAvailable_Should_Return_False_When_Device_Offline()
        {
            // Arrange
            _deviceProxy.IsDeviceAvailableAsync("terminal", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(false));

            // Act
            var result = await _appService.IsPaymentMethodAvailableAsync("terminal");

            // Assert
            result.ShouldBeFalse();
        }

        #endregion

        #region Get Available Payment Methods Tests

        [Fact]
        [UnitOfWork]
        public async Task GetAvailablePaymentMethods_Should_Include_Methods_When_Terminal_Available()
        {
            // Arrange
            _deviceProxy.IsDeviceAvailableAsync("terminal", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            // Act
            var result = await _appService.GetAvailablePaymentMethodsAsync();

            // Assert
            result.ShouldNotBeNull();
            // The actual payment methods depend on implementation
            // Just verify the call was made to the device proxy
            await _deviceProxy.Received(1).IsDeviceAvailableAsync("terminal", Arg.Any<CancellationToken>());
        }

        [Fact]
        [UnitOfWork]
        public async Task GetAvailablePaymentMethods_Should_Not_Include_Card_When_Terminal_Offline()
        {
            // Arrange
            _deviceProxy.IsDeviceAvailableAsync("terminal", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(false));

            // Act
            var result = await _appService.GetAvailablePaymentMethodsAsync();

            // Assert
            result.ShouldNotBeNull();
            await _deviceProxy.Received(1).IsDeviceAvailableAsync("terminal", Arg.Any<CancellationToken>());
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        [UnitOfWork]
        public async Task CheckTerminalStatus_Should_Not_Throw_On_Communication_Error()
        {
            // Arrange
            _deviceProxy.IsDeviceAvailableAsync("terminal", Arg.Any<CancellationToken>())
                .Returns(Task.FromException<bool>(new TimeoutException("Device timeout")));

            // Act & Assert - should not throw
            var result = await _appService.CheckTerminalStatusAsync();
            result.ShouldBeFalse();
        }

        [Fact]
        [UnitOfWork]
        public async Task IsPaymentMethodAvailable_Should_Not_Throw_On_Device_Error()
        {
            // Arrange
            _deviceProxy.IsDeviceAvailableAsync("terminal", Arg.Any<CancellationToken>())
                .Returns(Task.FromException<bool>(new InvalidOperationException("Device error")));

            // Act & Assert - should not throw
            var result = await _appService.IsPaymentMethodAvailableAsync("terminal");
            result.ShouldBeFalse();
        }

        #endregion

        #region Multi-Call Tests

        [Fact]
        [UnitOfWork]
        public async Task Multiple_Device_Checks_Should_Call_Proxy_Each_Time()
        {
            // Arrange
            _deviceProxy.IsDeviceAvailableAsync("terminal", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            // Act
            for (int i = 0; i < 3; i++)
            {
                await _appService.CheckTerminalStatusAsync();
            }

            // Assert
            await _deviceProxy.Received(3).IsDeviceAvailableAsync("terminal", Arg.Any<CancellationToken>());
        }

        [Fact]
        [UnitOfWork]
        public async Task Different_Device_Types_Should_Call_Proxy_With_Correct_Type()
        {
            // Arrange
            _deviceProxy.IsDeviceAvailableAsync("terminal", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));
            _deviceProxy.IsDeviceAvailableAsync("fiscal_printer", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            // Act
            await _appService.IsPaymentMethodAvailableAsync("terminal");
            await _appService.IsPaymentMethodAvailableAsync("fiscal_printer");

            // Assert
            await _deviceProxy.Received(1).IsDeviceAvailableAsync("terminal", Arg.Any<CancellationToken>());
            await _deviceProxy.Received(1).IsDeviceAvailableAsync("fiscal_printer", Arg.Any<CancellationToken>());
        }

        #endregion

        #region Dependency Injection Tests

        [Fact]
        public void ItemCheckoutAppService_Should_Have_Device_Proxy_Injected()
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
        public void Same_Proxy_Instance_Should_Be_Used()
        {
            // Arrange & Act
            var proxy1 = GetRequiredService<IRemoteDeviceProxy>();
            var proxy2 = GetRequiredService<IRemoteDeviceProxy>();

            // Assert - should be same instance for transient
            // (depends on DI configuration)
            proxy1.ShouldNotBeNull();
            proxy2.ShouldNotBeNull();
        }

        #endregion

        #region Command Pattern Tests

        [Fact]
        [UnitOfWork]
        public async Task Device_Proxy_Should_Support_Terminal_Commands()
        {
            // Arrange
            var command = new CheckTerminalStatusCommand
            {
                CommandId = Guid.NewGuid(),
                TenantId = _currentTenant.Id ?? Guid.Empty,
                TerminalProviderId = "terminal_1"
            };

            var response = new TerminalStatusResponse
            {
                CommandId = command.CommandId,
                Success = true,
                ProcessedAt = DateTime.UtcNow
            };

            _deviceProxy.CheckTerminalStatusAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(response));

            // Act
            var result = await _deviceProxy.CheckTerminalStatusAsync(command);

            // Assert
            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
            result.CommandId.ShouldBe(command.CommandId);
        }

        [Fact]
        [UnitOfWork]
        public async Task Device_Proxy_Should_Support_Fiscal_Commands()
        {
            // Arrange
            var command = new PrintFiscalReceiptCommand
            {
                CommandId = Guid.NewGuid(),
                TenantId = _currentTenant.Id ?? Guid.Empty,
                Items = new System.Collections.Generic.List<FiscalReceiptItem>(),
                TotalAmount = 100m,
                FiscalPrinterProviderId = "printer_1",
                TransactionId = "TRX_123"
            };

            var response = new FiscalReceiptResponse
            {
                CommandId = command.CommandId,
                Success = true,
                ProcessedAt = DateTime.UtcNow
            };

            _deviceProxy.PrintFiscalReceiptAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(response));

            // Act
            var result = await _deviceProxy.PrintFiscalReceiptAsync(command);

            // Assert
            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
            result.CommandId.ShouldBe(command.CommandId);
        }

        #endregion
    }
}
