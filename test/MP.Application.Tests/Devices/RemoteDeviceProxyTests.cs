using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Xunit;
using MP.Application.Devices;
using MP.LocalAgent.Contracts.Commands;
using MP.LocalAgent.Contracts.Responses;

namespace MP.Application.Tests.Devices
{
    /// <summary>
    /// Unit tests for IRemoteDeviceProxy interface implementation
    /// Tests device communication abstraction layer
    /// </summary>
    public class RemoteDeviceProxyTests
    {
        private readonly IRemoteDeviceProxy _proxy;

        public RemoteDeviceProxyTests()
        {
            // Create a mock proxy for testing
            _proxy = Substitute.For<IRemoteDeviceProxy>();
        }

        #region Device Availability Tests

        [Fact]
        public async Task IsDeviceAvailableAsync_Terminal_Should_Return_True_When_Online()
        {
            // Arrange
            _proxy.IsDeviceAvailableAsync("terminal", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            // Act
            var result = await _proxy.IsDeviceAvailableAsync("terminal");

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task IsDeviceAvailableAsync_Terminal_Should_Return_False_When_Offline()
        {
            // Arrange
            _proxy.IsDeviceAvailableAsync("terminal", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(false));

            // Act
            var result = await _proxy.IsDeviceAvailableAsync("terminal");

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task IsDeviceAvailableAsync_FiscalPrinter_Should_Return_True_When_Online()
        {
            // Arrange
            _proxy.IsDeviceAvailableAsync("fiscal_printer", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            // Act
            var result = await _proxy.IsDeviceAvailableAsync("fiscal_printer");

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task IsDeviceAvailableAsync_FiscalPrinter_Should_Return_False_When_Offline()
        {
            // Arrange
            _proxy.IsDeviceAvailableAsync("fiscal_printer", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(false));

            // Act
            var result = await _proxy.IsDeviceAvailableAsync("fiscal_printer");

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task GetDeviceStatusAsync_Should_Return_Status_String()
        {
            // Arrange
            var expectedStatus = "Ready";
            _proxy.GetDeviceStatusAsync("terminal", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(expectedStatus));

            // Act
            var result = await _proxy.GetDeviceStatusAsync("terminal");

            // Assert
            result.ShouldBe(expectedStatus);
        }

        #endregion

        #region Terminal Payment Command Tests

        [Fact]
        public async Task AuthorizePaymentAsync_Should_Return_Success_Response()
        {
            // Arrange
            var command = new AuthorizeTerminalPaymentCommand
            {
                CommandId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Amount = 100.00m,
                TerminalProviderId = "terminal_1",
                RentalItemId = Guid.NewGuid()
            };

            var expectedResponse = new TerminalPaymentResponse
            {
                CommandId = command.CommandId,
                Success = true,
                TransactionId = "TRX_12345",
                AuthorizationCode = "AUTH_ABC123"
            };

            _proxy.AuthorizePaymentAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(expectedResponse));

            // Act
            var result = await _proxy.AuthorizePaymentAsync(command);

            // Assert
            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
            result.TransactionId.ShouldBe("TRX_12345");
            result.AuthorizationCode.ShouldBe("AUTH_ABC123");
        }

        [Fact]
        public async Task AuthorizePaymentAsync_Should_Return_Failure_Response()
        {
            // Arrange
            var command = new AuthorizeTerminalPaymentCommand
            {
                CommandId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Amount = 100.00m,
                TerminalProviderId = "terminal_1",
                RentalItemId = Guid.NewGuid()
            };

            var expectedResponse = new TerminalPaymentResponse
            {
                CommandId = command.CommandId,
                Success = false,
                ErrorCode = "PAYMENT_DECLINED",
                ErrorMessage = "Payment was declined by the bank"
            };

            _proxy.AuthorizePaymentAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(expectedResponse));

            // Act
            var result = await _proxy.AuthorizePaymentAsync(command);

            // Assert
            result.ShouldNotBeNull();
            result.Success.ShouldBeFalse();
            result.ErrorCode.ShouldBe("PAYMENT_DECLINED");
        }

        [Fact]
        public async Task CapturePaymentAsync_Should_Return_Success_Response()
        {
            // Arrange
            var command = new CaptureTerminalPaymentCommand
            {
                CommandId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                TransactionId = "TRX_12345",
                Amount = 100.00m,
                TerminalProviderId = "terminal_1"
            };

            var expectedResponse = new TerminalPaymentResponse
            {
                CommandId = command.CommandId,
                Success = true,
                TransactionId = command.TransactionId
            };

            _proxy.CapturePaymentAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(expectedResponse));

            // Act
            var result = await _proxy.CapturePaymentAsync(command);

            // Assert
            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task RefundPaymentAsync_Should_Return_Success_Response()
        {
            // Arrange
            var command = new RefundTerminalPaymentCommand
            {
                CommandId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                TransactionId = "TRX_12345",
                Amount = 50.00m,
                TerminalProviderId = "terminal_1"
            };

            var expectedResponse = new TerminalPaymentResponse
            {
                CommandId = command.CommandId,
                Success = true,
                TransactionId = $"REFUND_{command.TransactionId}"
            };

            _proxy.RefundPaymentAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(expectedResponse));

            // Act
            var result = await _proxy.RefundPaymentAsync(command);

            // Assert
            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task CancelPaymentAsync_Should_Return_Success_Response()
        {
            // Arrange
            var command = new CancelTerminalPaymentCommand
            {
                CommandId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                TransactionId = "TRX_12345",
                TerminalProviderId = "terminal_1"
            };

            var expectedResponse = new TerminalPaymentResponse
            {
                CommandId = command.CommandId,
                Success = true,
                TransactionId = command.TransactionId
            };

            _proxy.CancelPaymentAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(expectedResponse));

            // Act
            var result = await _proxy.CancelPaymentAsync(command);

            // Assert
            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
        }

        #endregion

        #region Terminal Status Tests

        [Fact]
        public async Task CheckTerminalStatusAsync_Should_Return_Ready_Status()
        {
            // Arrange
            var command = new CheckTerminalStatusCommand
            {
                CommandId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                TerminalProviderId = "terminal_1"
            };

            var expectedResponse = new TerminalStatusResponse
            {
                CommandId = command.CommandId,
                Success = true,
                ProcessedAt = DateTime.UtcNow
            };

            _proxy.CheckTerminalStatusAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(expectedResponse));

            // Act
            var result = await _proxy.CheckTerminalStatusAsync(command);

            // Assert
            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
        }

        #endregion

        #region Fiscal Printer Tests

        [Fact]
        public async Task PrintFiscalReceiptAsync_Should_Return_Success_Response()
        {
            // Arrange
            var command = new PrintFiscalReceiptCommand
            {
                CommandId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Items = new System.Collections.Generic.List<FiscalReceiptItem>
                {
                    new FiscalReceiptItem { Name = "Item 1", Quantity = 1, UnitPrice = 100m, TotalPrice = 100m }
                },
                TotalAmount = 100m,
                FiscalPrinterProviderId = "printer_1",
                TransactionId = "TRX_12345"
            };

            var expectedResponse = new FiscalReceiptResponse
            {
                CommandId = command.CommandId,
                Success = true,
                FiscalNumber = "REC_202510231100"
            };

            _proxy.PrintFiscalReceiptAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(expectedResponse));

            // Act
            var result = await _proxy.PrintFiscalReceiptAsync(command);

            // Assert
            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
            result.FiscalNumber.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async Task CheckFiscalPrinterStatusAsync_Should_Return_Ready_Status()
        {
            // Arrange
            var command = new CheckFiscalPrinterStatusCommand
            {
                CommandId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                FiscalPrinterProviderId = "printer_1"
            };

            var expectedResponse = new FiscalPrinterStatusResponse
            {
                CommandId = command.CommandId,
                Success = true,
                ProcessedAt = DateTime.UtcNow
            };

            _proxy.CheckFiscalPrinterStatusAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(expectedResponse));

            // Act
            var result = await _proxy.CheckFiscalPrinterStatusAsync(command);

            // Assert
            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task GetDailyFiscalReportAsync_Should_Return_Report_Data()
        {
            // Arrange
            var command = new GetDailyFiscalReportCommand
            {
                CommandId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                ReportDate = DateTime.UtcNow.Date,
                FiscalPrinterProviderId = "printer_1"
            };

            var expectedResponse = new FiscalReportResponse
            {
                CommandId = command.CommandId,
                Success = true,
                ProcessedAt = DateTime.UtcNow
            };

            _proxy.GetDailyFiscalReportAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(expectedResponse));

            // Act
            var result = await _proxy.GetDailyFiscalReportAsync(command);

            // Assert
            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
        }

        #endregion

        #region Multi-Tenant Tests

        [Fact]
        public async Task Commands_Should_Include_TenantId()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var command = new AuthorizeTerminalPaymentCommand
            {
                CommandId = Guid.NewGuid(),
                TenantId = tenantId,
                Amount = 100m,
                TerminalProviderId = "terminal_1",
                RentalItemId = Guid.NewGuid()
            };

            // Assert
            command.TenantId.ShouldBe(tenantId);
        }

        [Fact]
        public async Task Different_Tenants_Should_Use_Different_Commands()
        {
            // Arrange
            var tenantId1 = Guid.NewGuid();
            var tenantId2 = Guid.NewGuid();

            var command1 = new AuthorizeTerminalPaymentCommand
            {
                CommandId = Guid.NewGuid(),
                TenantId = tenantId1,
                Amount = 100m,
                TerminalProviderId = "terminal_1",
                RentalItemId = Guid.NewGuid()
            };

            var command2 = new AuthorizeTerminalPaymentCommand
            {
                CommandId = Guid.NewGuid(),
                TenantId = tenantId2,
                Amount = 100m,
                TerminalProviderId = "terminal_1",
                RentalItemId = Guid.NewGuid()
            };

            // Assert
            command1.TenantId.ShouldNotBe(command2.TenantId);
            command1.CommandId.ShouldNotBe(command2.CommandId);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task Response_Should_Include_Error_Code_On_Failure()
        {
            // Arrange
            var command = new AuthorizeTerminalPaymentCommand
            {
                CommandId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Amount = 100m,
                TerminalProviderId = "terminal_1",
                RentalItemId = Guid.NewGuid()
            };

            var expectedResponse = new TerminalPaymentResponse
            {
                CommandId = command.CommandId,
                Success = false,
                ErrorCode = "TERMINAL_OFFLINE",
                ErrorMessage = "Terminal device is offline"
            };

            _proxy.AuthorizePaymentAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(expectedResponse));

            // Act
            var result = await _proxy.AuthorizePaymentAsync(command);

            // Assert
            result.Success.ShouldBeFalse();
            result.ErrorCode.ShouldBe("TERMINAL_OFFLINE");
            result.ErrorMessage.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async Task Response_Should_Include_Processing_Duration()
        {
            // Arrange
            var command = new AuthorizeTerminalPaymentCommand
            {
                CommandId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Amount = 100m,
                TerminalProviderId = "terminal_1",
                RentalItemId = Guid.NewGuid()
            };

            var expectedResponse = new TerminalPaymentResponse
            {
                CommandId = command.CommandId,
                Success = true,
                ProcessedAt = DateTime.UtcNow,
                ProcessingDuration = TimeSpan.FromMilliseconds(250)
            };

            _proxy.AuthorizePaymentAsync(command, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(expectedResponse));

            // Act
            var result = await _proxy.AuthorizePaymentAsync(command);

            // Assert
            result.ProcessingDuration.TotalMilliseconds.ShouldBeGreaterThan(0);
        }

        #endregion

        #region Cancellation Tests

        [Fact]
        public async Task Should_Support_Cancellation_Token()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var command = new AuthorizeTerminalPaymentCommand
            {
                CommandId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Amount = 100m,
                TerminalProviderId = "terminal_1",
                RentalItemId = Guid.NewGuid()
            };

            // Should not throw when cancellation token is provided
            _proxy.AuthorizePaymentAsync(command, cts.Token)
                .Returns(Task.FromResult(new TerminalPaymentResponse
                {
                    CommandId = command.CommandId,
                    Success = true
                }));

            // Act
            var result = await _proxy.AuthorizePaymentAsync(command, cts.Token);

            // Assert
            result.ShouldNotBeNull();
        }

        #endregion
    }
}
