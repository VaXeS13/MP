using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MP.LocalAgent.Configuration;
using MP.LocalAgent.Contracts.Commands;
using MP.LocalAgent.Contracts.Responses;
using MP.LocalAgent.Interfaces;
using MP.LocalAgent.Exceptions;

namespace MP.LocalAgent.Services
{
    /// <summary>
    /// Mock fiscal printer service for testing and development
    /// </summary>
    public class MockFiscalPrinterService : IFiscalPrinterService
    {
        private readonly ILogger<MockFiscalPrinterService> _logger;
        private readonly Random _random = new();
        private bool _isInitialized;
        private bool _hasPaper = true;
        private bool _fiscalMemoryOk = true;
        private DateTime _lastActivity = DateTime.UtcNow;
        private string _lastFiscalNumber = "FISCAL-000001";
        private int _receiptCounter = 1;

        public event EventHandler<FiscalPrinterStatusEventArgs>? StatusChanged;
        public event EventHandler<ReceiptPrintedEventArgs>? ReceiptPrinted;
        public event EventHandler<FiscalMemoryWarningEventArgs>? FiscalMemoryWarning;

        public MockFiscalPrinterService(ILogger<MockFiscalPrinterService> logger)
        {
            _logger = logger;
        }

        public async Task<FiscalReceiptResponse> PrintReceiptAsync(PrintFiscalReceiptCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Mock: Printing fiscal receipt for transaction {TransactionId} - Total: {TotalAmount}",
                command.TransactionId, command.TotalAmount);

            if (!_isInitialized)
            {
                throw new FiscalPrinterException("Fiscal printer is not initialized")
                {
                    IsFiscalMemoryError = true
                };
            }

            if (!_hasPaper)
            {
                throw new FiscalPrinterException("Printer is out of paper");
            }

            if (!_fiscalMemoryOk)
            {
                throw new FiscalPrinterException("Fiscal memory error")
                {
                    FiscalErrorCode = "MEMORY_ERROR",
                    IsFiscalMemoryError = true
                };
            }

            try
            {
                // Simulate receipt printing time
                var printingTime = TimeSpan.FromMilliseconds(_random.Next(2000, 5000));
                await Task.Delay(printingTime, cancellationToken);

                // Generate fiscal number
                var fiscalNumber = $"FISCAL-{_receiptCounter:D6}";
                _receiptCounter++;
                _lastFiscalNumber = fiscalNumber;
                _lastActivity = DateTime.UtcNow;

                // Simulate paper consumption (1% chance of running out)
                if (_random.NextDouble() < 0.01)
                {
                    _hasPaper = false;
                    _logger.LogWarning("Mock: Printer ran out of paper");
                }

                // Calculate totals
                var totalTax = command.TotalAmount * 0.23m; // 23% VAT
                var itemCount = command.Items.Count;

                _logger.LogInformation("Mock: Fiscal receipt printed successfully - Fiscal Number: {FiscalNumber}", fiscalNumber);

                var response = new FiscalReceiptResponse
                {
                    CommandId = command.CommandId,
                    Success = true,
                    FiscalNumber = fiscalNumber,
                    FiscalDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    FiscalTime = DateTime.UtcNow.ToString("HH:mm:ss"),
                    TotalAmount = command.TotalAmount,
                    TotalTax = totalTax,
                    ReceiptNumber = _receiptCounter - 1,
                    CashRegisterId = "MOCK-CR-001",
                    ProviderData = new()
                    {
                        ["MockPrintingTime"] = printingTime.TotalMilliseconds,
                        ["MockPaperRemaining"] = _random.Next(10, 100),
                        ["MockFiscalMemoryUsage"] = _random.Next(1, 50)
                    }
                };

                // Fire receipt printed event
                ReceiptPrinted?.Invoke(this, new ReceiptPrintedEventArgs
                {
                    FiscalNumber = fiscalNumber,
                    TotalAmount = command.TotalAmount,
                    ItemCount = itemCount,
                    PaymentMethod = command.PaymentMethod,
                    PrintedAt = DateTime.UtcNow,
                    Success = true
                });

                // Check fiscal memory and issue warning if needed
                var memoryUsage = _random.Next(1, 100);
                if (memoryUsage > 80)
                {
                    FiscalMemoryWarning?.Invoke(this, new FiscalMemoryWarningEventArgs
                    {
                        UsagePercent = memoryUsage,
                        Message = $"Fiscal memory usage is critical: {memoryUsage}%",
                        IsCritical = memoryUsage > 90
                    });
                }

                return response;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Mock: Receipt printing cancelled");
                throw new CommandTimeoutException("Receipt printing timed out")
                {
                    CommandId = command.CommandId,
                    Timeout = command.Timeout
                };
            }
        }

        public async Task<SimpleCommandResponse> PrintNonFiscalDocumentAsync(PrintNonFiscalDocumentCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Mock: Printing non-fiscal document with {LineCount} lines", command.Lines.Length);

            if (!_hasPaper)
            {
                throw new FiscalPrinterException("Printer is out of paper");
            }

            try
            {
                // Simulate document printing time
                await Task.Delay(_random.Next(1000, 3000), cancellationToken);

                _lastActivity = DateTime.UtcNow;

                _logger.LogInformation("Mock: Non-fiscal document printed successfully");

                return new SimpleCommandResponse
                {
                    CommandId = command.CommandId,
                    Success = true,
                    Message = $"Printed {command.Lines.Length} lines successfully"
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Mock: Non-fiscal document printing cancelled");
                throw;
            }
        }

        public async Task<FiscalReportResponse> GetDailyReportAsync(GetDailyFiscalReportCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Mock: Generating daily fiscal report for {ReportDate}", command.ReportDate.ToString("yyyy-MM-dd"));

            try
            {
                // Simulate report generation time
                await Task.Delay(_random.Next(2000, 4000), cancellationToken);

                // Generate mock report data
                var totalSales = _random.Next(1000, 10000);
                var totalTax = totalSales * 0.23m;
                var receiptCount = _random.Next(10, 100);

                var salesByTaxRate = new Dictionary<string, decimal>
                {
                    ["A"] = totalSales * 0.6m,
                    ["B"] = totalSales * 0.4m
                };

                var salesByPaymentMethod = new Dictionary<string, decimal>
                {
                    ["Cash"] = totalSales * 0.3m,
                    ["Card"] = totalSales * 0.7m
                };

                _logger.LogInformation("Mock: Daily report generated successfully");

                return new FiscalReportResponse
                {
                    CommandId = command.CommandId,
                    Success = true,
                    ReportDate = command.ReportDate,
                    ReportType = "Daily",
                    TotalSales = totalSales,
                    TotalTax = totalTax,
                    ReceiptCount = receiptCount,
                    SalesByTaxRate = salesByTaxRate,
                    SalesByPaymentMethod = salesByPaymentMethod,
                    ReportNumber = $"RPT-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    ProviderData = new()
                    {
                        ["MockReportGenerationTime"] = DateTime.UtcNow
                    }
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Mock: Daily report generation cancelled");
                throw;
            }
        }

        public async Task<SimpleCommandResponse> CancelLastReceiptAsync(CancelLastFiscalReceiptCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Mock: Cancelling last fiscal receipt - Reason: {Reason}", command.Reason);

            try
            {
                // Simulate cancellation processing time
                await Task.Delay(_random.Next(1000, 2000), cancellationToken);

                _lastActivity = DateTime.UtcNow;

                _logger.LogInformation("Mock: Last fiscal receipt cancelled successfully");

                return new SimpleCommandResponse
                {
                    CommandId = command.CommandId,
                    Success = true,
                    Message = $"Last receipt cancelled: {command.Reason}"
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Mock: Receipt cancellation cancelled");
                throw;
            }
        }

        public async Task<FiscalPrinterStatusResponse> CheckStatusAsync(CheckFiscalPrinterStatusCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Mock: Checking fiscal printer status");

            // Simulate status check
            await Task.Delay(_random.Next(200, 1000), cancellationToken);

            var memoryUsage = _random.Next(1, 100);
            var memoryWarning = memoryUsage > 80;

            return new FiscalPrinterStatusResponse
            {
                CommandId = command.CommandId,
                Success = true,
                IsOnline = _isInitialized,
                HasPaper = _hasPaper,
                FiscalMemoryOk = _fiscalMemoryOk,
                FiscalMemoryUsagePercent = memoryUsage,
                LastReceiptDate = _lastActivity,
                LastFiscalNumber = _lastFiscalNumber,
                IsInFiscalMode = _isInitialized,
                Model = "MockFiscalPrinter 2000",
                SerialNumber = "MOCK-FP-001",
                FirmwareVersion = "2.1.0-mock",
                LastZReportDate = DateTime.UtcNow.AddDays(-1),
                ProviderData = new()
                {
                    ["MockStatus"] = _isInitialized ? "Ready" : "Not Ready",
                    ["MockTemperature"] = _random.Next(20, 40),
                    ["MockMotorHours"] = _random.Next(100, 1000)
                }
            };
        }

        public async Task<bool> InitializeAsync(DeviceConfiguration config)
        {
            _logger.LogInformation("Mock: Initializing fiscal printer with provider {ProviderId}", config.ProviderId);

            try
            {
                // Simulate initialization time
                await Task.Delay(_random.Next(2000, 5000));

                _isInitialized = true;
                _hasPaper = true;
                _fiscalMemoryOk = true;
                _lastActivity = DateTime.UtcNow;

                _logger.LogInformation("Mock: Fiscal printer initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mock: Failed to initialize fiscal printer");
                return false;
            }
        }

        public async Task<bool> IsReadyAsync()
        {
            // Simulate ready check
            await Task.Delay(_random.Next(100, 500));
            return _isInitialized && _hasPaper && _fiscalMemoryOk;
        }

        public async Task<FiscalPrinterInfo> GetPrinterInfoAsync()
        {
            var memoryUsage = _random.Next(1, 100);

            return await Task.FromResult(new FiscalPrinterInfo
            {
                Model = "MockFiscalPrinter 2000",
                SerialNumber = "MOCK-FP-001",
                FirmwareVersion = "2.1.0-mock",
                ProviderId = "mock",
                ConnectionType = "Mock",
                IsInitialized = _isInitialized,
                IsOnline = _isInitialized,
                HasPaper = _hasPaper,
                FiscalMemoryOk = _fiscalMemoryOk,
                FiscalMemoryUsagePercent = memoryUsage,
                LastFiscalNumber = _lastFiscalNumber,
                LastReceiptDate = _lastActivity,
                LastZReportDate = DateTime.UtcNow.AddDays(-1),
                Region = "PL",
                IsInFiscalMode = _isInitialized,
                LastActivity = _lastActivity
            });
        }

        public async Task<bool> PerformHealthCheckAsync()
        {
            _logger.LogDebug("Mock: Performing fiscal printer health check");

            // Simulate health check
            await Task.Delay(_random.Next(500, 2000));

            var isHealthy = _isInitialized && _hasPaper && _fiscalMemoryOk;

            if (!isHealthy && _isInitialized)
            {
                // Try to recover
                if (!_hasPaper && _random.NextDouble() > 0.5)
                {
                    _hasPaper = true;
                    _logger.LogInformation("Mock: Paper restored during health check");
                }

                if (!_fiscalMemoryOk && _random.NextDouble() > 0.8)
                {
                    _fiscalMemoryOk = true;
                    _logger.LogInformation("Mock: Fiscal memory restored during health check");
                }
            }

            return isHealthy;
        }

        public async Task<string?> GetLastFiscalNumberAsync()
        {
            await Task.Delay(_random.Next(100, 300));
            return _lastFiscalNumber;
        }

        public async Task<bool> CheckFiscalMemoryHealthAsync()
        {
            await Task.Delay(_random.Next(200, 800));
            return _fiscalMemoryOk && _random.NextDouble() > 0.05; // 5% chance of memory issue
        }
    }
}