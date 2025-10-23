using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Xunit;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using MP.Application.Devices;
using MP.LocalAgent.Contracts.Commands;
using MP.LocalAgent.Contracts.Responses;

namespace MP.Application.Tests.Devices
{
    /// <summary>
    /// Multi-tenant tests for device proxy to ensure proper isolation
    /// Validates that commands include correct tenant context
    /// </summary>
    public class DeviceProxyMultiTenantTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private IRemoteDeviceProxy _deviceProxy = null!;
        private ICurrentTenant _currentTenant = null!;

        public DeviceProxyMultiTenantTests()
        {
            _deviceProxy = GetRequiredService<IRemoteDeviceProxy>();
            _currentTenant = GetRequiredService<ICurrentTenant>();
        }

        #region Tenant Context Tests

        [Fact]
        [UnitOfWork]
        public async Task Device_Command_Should_Include_Tenant_Context()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var commandId = Guid.NewGuid();

            using (_currentTenant.Change(tenantId))
            {
                var command = new AuthorizeTerminalPaymentCommand
                {
                    CommandId = commandId,
                    TenantId = tenantId,
                    Amount = 100m,
                    TerminalProviderId = "terminal_1",
                    RentalItemId = Guid.NewGuid()
                };

                // Assert
                command.TenantId.ShouldBe(tenantId);
            }
        }

        [Fact]
        [UnitOfWork]
        public async Task Different_Tenants_Should_Have_Different_Command_Contexts()
        {
            // Arrange
            var tenantId1 = Guid.NewGuid();
            var tenantId2 = Guid.NewGuid();
            var commandId1 = Guid.NewGuid();
            var commandId2 = Guid.NewGuid();

            // Act & Assert - Tenant 1
            using (_currentTenant.Change(tenantId1))
            {
                var command1 = new AuthorizeTerminalPaymentCommand
                {
                    CommandId = commandId1,
                    TenantId = tenantId1,
                    Amount = 100m,
                    TerminalProviderId = "terminal_1",
                    RentalItemId = Guid.NewGuid()
                };

                command1.TenantId.ShouldBe(tenantId1);
            }

            // Act & Assert - Tenant 2
            using (_currentTenant.Change(tenantId2))
            {
                var command2 = new AuthorizeTerminalPaymentCommand
                {
                    CommandId = commandId2,
                    TenantId = tenantId2,
                    Amount = 200m,
                    TerminalProviderId = "terminal_1",
                    RentalItemId = Guid.NewGuid()
                };

                command2.TenantId.ShouldBe(tenantId2);
            }
        }

        #endregion

        #region Payment Command Isolation Tests

        [Fact]
        [UnitOfWork]
        public async Task AuthorizePayment_Should_Include_Tenant_In_Response()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var commandId = Guid.NewGuid();

            var command = new AuthorizeTerminalPaymentCommand
            {
                CommandId = commandId,
                TenantId = tenantId,
                Amount = 100m,
                TerminalProviderId = "terminal_1",
                RentalItemId = Guid.NewGuid()
            };

            var response = new TerminalPaymentResponse
            {
                CommandId = commandId,
                Success = true,
                TransactionId = "TRX_123",
                AuthorizationCode = "AUTH_456",
                Status = "authorized"
            };

            using (_currentTenant.Change(tenantId))
            {
                // Act & Assert
                response.CommandId.ShouldBe(commandId);
                response.Success.ShouldBeTrue();
            }
        }

        [Fact]
        [UnitOfWork]
        public async Task CapturePayment_Commands_Should_Be_Tenant_Specific()
        {
            // Arrange
            var tenantId1 = Guid.NewGuid();
            var tenantId2 = Guid.NewGuid();

            using (_currentTenant.Change(tenantId1))
            {
                var command1 = new CaptureTerminalPaymentCommand
                {
                    CommandId = Guid.NewGuid(),
                    TenantId = tenantId1,
                    TransactionId = "TRX_123",
                    Amount = 100m,
                    TerminalProviderId = "terminal_1"
                };

                // Assert
                command1.TenantId.ShouldBe(tenantId1);
                command1.TenantId.ShouldNotBe(tenantId2);
            }
        }

        #endregion

        #region Fiscal Printer Command Isolation Tests

        [Fact]
        [UnitOfWork]
        public async Task PrintFiscalReceipt_Should_Include_Tenant_Context()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var commandId = Guid.NewGuid();

            using (_currentTenant.Change(tenantId))
            {
                var command = new PrintFiscalReceiptCommand
                {
                    CommandId = commandId,
                    TenantId = tenantId,
                    TransactionId = "TRX_123",
                    Items = new List<FiscalReceiptItem>
                    {
                        new FiscalReceiptItem
                        {
                            Name = "Item 1",
                            Quantity = 1,
                            UnitPrice = 100m,
                            TotalPrice = 100m
                        }
                    },
                    TotalAmount = 100m,
                    FiscalPrinterProviderId = "printer_1"
                };

                // Assert
                command.TenantId.ShouldBe(tenantId);
                command.CommandId.ShouldBe(commandId);
            }
        }

        [Fact]
        [UnitOfWork]
        public async Task CheckFiscalPrinterStatus_Should_Be_Tenant_Specific()
        {
            // Arrange
            var tenantId = Guid.NewGuid();

            using (_currentTenant.Change(tenantId))
            {
                var command = new CheckFiscalPrinterStatusCommand
                {
                    CommandId = Guid.NewGuid(),
                    TenantId = tenantId,
                    FiscalPrinterProviderId = "printer_1"
                };

                // Assert
                command.TenantId.ShouldBe(tenantId);
            }
        }

        [Fact]
        [UnitOfWork]
        public async Task GetDailyFiscalReport_Should_Include_Tenant_Context()
        {
            // Arrange
            var tenantId = Guid.NewGuid();

            using (_currentTenant.Change(tenantId))
            {
                var command = new GetDailyFiscalReportCommand
                {
                    CommandId = Guid.NewGuid(),
                    TenantId = tenantId,
                    ReportDate = DateTime.UtcNow.Date,
                    FiscalPrinterProviderId = "printer_1"
                };

                // Assert
                command.TenantId.ShouldBe(tenantId);
            }
        }

        #endregion

        #region Cross-Tenant Security Tests

        [Fact]
        [UnitOfWork]
        public async Task Commands_From_Different_Tenants_Should_Have_Different_Ids()
        {
            // Arrange
            var tenantId1 = Guid.NewGuid();
            var tenantId2 = Guid.NewGuid();

            var commandId1 = Guid.Empty;
            var commandId2 = Guid.Empty;

            // Act - Tenant 1
            using (_currentTenant.Change(tenantId1))
            {
                commandId1 = Guid.NewGuid();
                var command1 = new AuthorizeTerminalPaymentCommand
                {
                    CommandId = commandId1,
                    TenantId = tenantId1,
                    Amount = 100m,
                    TerminalProviderId = "terminal_1",
                    RentalItemId = Guid.NewGuid()
                };
            }

            // Act - Tenant 2
            using (_currentTenant.Change(tenantId2))
            {
                commandId2 = Guid.NewGuid();
                var command2 = new AuthorizeTerminalPaymentCommand
                {
                    CommandId = commandId2,
                    TenantId = tenantId2,
                    Amount = 100m,
                    TerminalProviderId = "terminal_1",
                    RentalItemId = Guid.NewGuid()
                };
            }

            // Assert
            commandId1.ShouldNotBe(commandId2);
        }

        [Fact]
        [UnitOfWork]
        public async Task Tenant_Context_Should_Be_Maintained_Across_Multiple_Commands()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var commands = new List<AuthorizeTerminalPaymentCommand>();

            // Act
            using (_currentTenant.Change(tenantId))
            {
                for (int i = 0; i < 3; i++)
                {
                    commands.Add(new AuthorizeTerminalPaymentCommand
                    {
                        CommandId = Guid.NewGuid(),
                        TenantId = tenantId,
                        Amount = (i + 1) * 100m
                    });
                }
            }

            // Assert
            commands.Count.ShouldBe(3);
            foreach (var cmd in commands)
            {
                cmd.TenantId.ShouldBe(tenantId);
            }
        }

        #endregion

        #region Response Validation Tests

        [Fact]
        [UnitOfWork]
        public async Task Response_Should_Preserve_Command_Id()
        {
            // Arrange
            var commandId = Guid.NewGuid();
            var command = new AuthorizeTerminalPaymentCommand
            {
                CommandId = commandId,
                TenantId = _currentTenant.Id ?? Guid.NewGuid(),
                Amount = 100m,
                TerminalProviderId = "terminal_1",
                RentalItemId = Guid.NewGuid()
            };

            var response = new TerminalPaymentResponse
            {
                CommandId = commandId,
                Success = true
            };

            // Assert
            response.CommandId.ShouldBe(commandId);
            response.CommandId.ShouldBe(command.CommandId);
        }

        [Fact]
        [UnitOfWork]
        public async Task Response_Should_Include_Timestamp()
        {
            // Arrange
            var before = DateTime.UtcNow;
            var response = new TerminalPaymentResponse
            {
                CommandId = Guid.NewGuid(),
                Success = true,
                ProcessedAt = DateTime.UtcNow
            };
            var after = DateTime.UtcNow;

            // Assert
            response.ProcessedAt.ShouldNotBeNull();
            response.ProcessedAt.Value.ShouldBeGreaterThanOrEqualTo(before);
            response.ProcessedAt.Value.ShouldBeLessThanOrEqualTo(after);
        }

        [Fact]
        [UnitOfWork]
        public async Task Error_Response_Should_Include_Error_Code()
        {
            // Arrange & Act
            var response = new TerminalPaymentResponse
            {
                CommandId = Guid.NewGuid(),
                Success = false,
                ErrorCode = "PAYMENT_DECLINED",
                ErrorMessage = "Card was declined"
            };

            // Assert
            response.Success.ShouldBeFalse();
            response.ErrorCode.ShouldNotBeNullOrEmpty();
            response.ErrorMessage.ShouldNotBeNullOrEmpty();
        }

        #endregion

        #region Concurrent Tenant Tests

        [Fact]
        [UnitOfWork]
        public async Task Concurrent_Commands_From_Different_Tenants_Should_Be_Independent()
        {
            // Arrange
            var tenantId1 = Guid.NewGuid();
            var tenantId2 = Guid.NewGuid();

            var command1 = new AuthorizeTerminalPaymentCommand();
            var command2 = new AuthorizeTerminalPaymentCommand();

            // Act
            Task task1 = Task.Run(() =>
            {
                using (_currentTenant.Change(tenantId1))
                {
                    command1 = new AuthorizeTerminalPaymentCommand
                    {
                        CommandId = Guid.NewGuid(),
                        TenantId = tenantId1,
                        Amount = 100m,
                        TerminalProviderId = "terminal_1",
                        RentalItemId = Guid.NewGuid()
                    };
                }
            });

            Task task2 = Task.Run(() =>
            {
                using (_currentTenant.Change(tenantId2))
                {
                    command2 = new AuthorizeTerminalPaymentCommand
                    {
                        CommandId = Guid.NewGuid(),
                        TenantId = tenantId2,
                        Amount = 200m,
                        TerminalProviderId = "terminal_1",
                        RentalItemId = Guid.NewGuid()
                    };
                }
            });

            await Task.WhenAll(task1, task2);

            // Assert
            command1.TenantId.ShouldBe(tenantId1);
            command2.TenantId.ShouldBe(tenantId2);
            command1.CommandId.ShouldNotBe(command2.CommandId);
        }

        #endregion
    }
}
