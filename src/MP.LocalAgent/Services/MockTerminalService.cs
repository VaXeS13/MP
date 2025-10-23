using System;
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
    /// Mock terminal service for testing and development
    /// </summary>
    public class MockTerminalService : ITerminalService
    {
        private readonly ILogger<MockTerminalService> _logger;
        private readonly Random _random = new();
        private bool _isInitialized;
        private bool _isReady;
        private DateTime _lastActivity = DateTime.UtcNow;

        public event EventHandler<TerminalStatusEventArgs>? StatusChanged;
        public event EventHandler<PaymentProcessedEventArgs>? PaymentProcessed;

        public MockTerminalService(ILogger<MockTerminalService> logger)
        {
            _logger = logger;
        }

        public async Task<TerminalPaymentResponse> AuthorizePaymentAsync(AuthorizeTerminalPaymentCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Mock: Authorizing payment of {Amount} {Currency} for {Description}",
                command.Amount, command.Currency, command.Description);

            if (!_isReady)
            {
                throw new PaymentTerminalException("Terminal is not ready") { IsCardDeclined = false };
            }

            try
            {
                // Simulate terminal processing time
                var processingTime = TimeSpan.FromMilliseconds(_random.Next(1000, 4000));
                await Task.Delay(processingTime, cancellationToken);

                // Simulate success/failure (90% success rate)
                var isSuccess = _random.NextDouble() > 0.1;
                var transactionId = $"MOCK-{Guid.NewGuid():N}";
                var authCode = _random.Next(100000, 999999).ToString();

                if (isSuccess)
                {
                    _logger.LogInformation("Mock: Payment authorized successfully - Transaction: {TransactionId}", transactionId);

                    var response = new TerminalPaymentResponse
                    {
                        CommandId = command.CommandId,
                        Success = true,
                        TransactionId = transactionId,
                        AuthorizationCode = authCode,
                        Status = "captured",
                        Amount = command.Amount,
                        Currency = command.Currency,
                        ProcessedAt = DateTime.UtcNow,
                        Timestamp = DateTime.UtcNow,
                        CardType = GetRandomCardType(),
                        LastFourDigits = _random.Next(1000, 9999).ToString(),
                        ProviderData = new()
                        {
                            ["MockProcessingTime"] = processingTime.TotalMilliseconds,
                            ["MockTerminalId"] = "MOCK-TERMINAL-001"
                        }
                    };

                    _lastActivity = DateTime.UtcNow;

                    // Fire payment processed event
                    PaymentProcessed?.Invoke(this, new PaymentProcessedEventArgs
                    {
                        TransactionId = transactionId,
                        Amount = command.Amount,
                        Success = true,
                        ProcessedAt = DateTime.UtcNow,
                        CardType = response.CardType,
                        LastFourDigits = response.LastFourDigits
                    });

                    return response;
                }
                else
                {
                    var failureReasons = new[] { "Insufficient funds", "Card declined", "Network error", "Invalid card" };
                    var failureReason = failureReasons[_random.Next(failureReasons.Length)];

                    _logger.LogWarning("Mock: Payment failed - {Reason}", failureReason);

                    PaymentProcessed?.Invoke(this, new PaymentProcessedEventArgs
                    {
                        TransactionId = transactionId,
                        Amount = command.Amount,
                        Success = false,
                        ErrorMessage = failureReason,
                        ProcessedAt = DateTime.UtcNow
                    });

                    throw new PaymentTerminalException($"Payment failed: {failureReason}")
                    {
                        TransactionId = transactionId,
                        IsCardDeclined = true
                    };
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Mock: Payment authorization cancelled");
                throw new CommandTimeoutException("Payment authorization timed out")
                {
                    CommandId = command.CommandId,
                    Timeout = command.Timeout
                };
            }
        }

        public async Task<TerminalPaymentResponse> CapturePaymentAsync(CaptureTerminalPaymentCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Mock: Capturing payment {TransactionId} for {Amount} {Currency}",
                command.TransactionId, command.Amount, "PLN");

            // Simulate capture processing
            await Task.Delay(_random.Next(500, 2000), cancellationToken);

            return new TerminalPaymentResponse
            {
                CommandId = command.CommandId,
                Success = true,
                TransactionId = command.TransactionId,
                Status = "captured",
                Amount = command.Amount,
                Currency = "PLN",
                ProcessedAt = DateTime.UtcNow,
                ProviderData = new()
                {
                    ["MockCaptureTime"] = DateTime.UtcNow
                }
            };
        }

        public async Task<TerminalPaymentResponse> RefundPaymentAsync(RefundTerminalPaymentCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Mock: Refunding payment {TransactionId} for {Amount} {Currency} - Reason: {Reason}",
                command.TransactionId, command.Amount, "PLN", command.Reason);

            // Simulate refund processing
            await Task.Delay(_random.Next(1000, 3000), cancellationToken);

            return new TerminalPaymentResponse
            {
                CommandId = command.CommandId,
                Success = true,
                TransactionId = command.TransactionId,
                Status = "refunded",
                Amount = command.Amount,
                Currency = "PLN",
                ProcessedAt = DateTime.UtcNow,
                ProviderData = new()
                {
                    ["MockRefundReason"] = command.Reason ?? "Customer request"
                }
            };
        }

        public async Task<TerminalPaymentResponse> CancelPaymentAsync(CancelTerminalPaymentCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Mock: Cancelling payment {TransactionId}", command.TransactionId);

            // Simulate cancellation processing
            await Task.Delay(_random.Next(500, 1500), cancellationToken);

            return new TerminalPaymentResponse
            {
                CommandId = command.CommandId,
                Success = true,
                TransactionId = command.TransactionId,
                Status = "cancelled",
                ProcessedAt = DateTime.UtcNow,
                ProviderData = new()
                {
                    ["MockCancelledAt"] = DateTime.UtcNow
                }
            };
        }

        public async Task<TerminalStatusResponse> CheckStatusAsync(CheckTerminalStatusCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Mock: Checking terminal status");

            // Simulate status check
            await Task.Delay(_random.Next(200, 1000), cancellationToken);

            return new TerminalStatusResponse
            {
                CommandId = command.CommandId,
                Success = true,
                IsOnline = _isReady,
                IsReady = _isReady,
                Model = "MockTerminal Pro X1",
                SerialNumber = "MOCK-SN-001",
                FirmwareVersion = "1.0.0-mock",
                LastActivity = _lastActivity,
                DeviceStatus = new()
                {
                    ["MockStatus"] = _isReady ? "Ready" : "Not Ready",
                    ["MockBatteryLevel"] = _random.Next(20, 100),
                    ["MockPaperLevel"] = _random.Next(50, 100)
                }
            };
        }

        public async Task<bool> InitializeAsync(DeviceConfiguration config)
        {
            _logger.LogInformation("Mock: Initializing terminal with provider {ProviderId}", config.ProviderId);

            try
            {
                // Simulate initialization time
                await Task.Delay(_random.Next(1000, 3000));

                _isInitialized = true;
                _isReady = true;
                _lastActivity = DateTime.UtcNow;

                _logger.LogInformation("Mock: Terminal initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mock: Failed to initialize terminal");
                return false;
            }
        }

        public async Task<bool> IsReadyAsync()
        {
            // Simulate ready check
            await Task.Delay(_random.Next(100, 500));
            return _isReady && _isInitialized;
        }

        public async Task<TerminalInfo> GetTerminalInfoAsync()
        {
            return await Task.FromResult(new TerminalInfo
            {
                Model = "MockTerminal Pro X1",
                SerialNumber = "MOCK-SN-001",
                FirmwareVersion = "1.0.0-mock",
                ProviderId = "mock",
                ConnectionType = "Mock",
                IsInitialized = _isInitialized,
                IsOnline = _isReady,
                LastActivity = _lastActivity,
                SupportedPaymentMethods = "Credit Card, Debit Card, Mobile Payment"
            });
        }

        public async Task<bool> PerformHealthCheckAsync()
        {
            _logger.LogDebug("Mock: Performing terminal health check");

            // Simulate health check
            await Task.Delay(_random.Next(500, 2000));

            var isHealthy = _isInitialized && _isReady;

            if (!isHealthy && _isInitialized)
            {
                // Try to recover
                _isReady = true;
                _logger.LogInformation("Mock: Terminal recovered during health check");
            }

            return isHealthy;
        }

        private string GetRandomCardType()
        {
            var cardTypes = new[] { "Visa", "Mastercard", "Maestro", "American Express", "Diners Club" };
            return cardTypes[_random.Next(cardTypes.Length)];
        }
    }
}