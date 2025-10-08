using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using MP.Domain.Terminals;

namespace MP.Application.Terminals
{
    /// <summary>
    /// Mock terminal provider for development and testing
    /// Simulates card terminal behavior without actual hardware
    /// </summary>
    public class MockTerminalProvider : ITerminalPaymentProvider, ITransientDependency
    {
        private readonly ILogger<MockTerminalProvider> _logger;
        private TenantTerminalSettings? _settings;

        public string ProviderId => "mock";
        public string DisplayName => "Mock Terminal (Development)";
        public string Description => "Mock payment terminal for development and testing purposes";

        public MockTerminalProvider(ILogger<MockTerminalProvider> logger)
        {
            _logger = logger;
        }

        public Task InitializeAsync(TenantTerminalSettings settings)
        {
            _settings = settings;
            _logger.LogInformation("Mock terminal initialized for tenant {TenantId}", settings.TenantId);
            return Task.CompletedTask;
        }

        public async Task<TerminalPaymentResult> AuthorizePaymentAsync(TerminalPaymentRequest request)
        {
            _logger.LogInformation(
                "Mock terminal: Authorizing payment of {Amount} {Currency} for item {ItemId}",
                request.Amount, request.Currency, request.RentalItemId);

            // Simulate terminal processing delay
            await Task.Delay(1500);

            var transactionId = $"MOCK-{Guid.NewGuid():N}";

            // Simulate 95% success rate
            var random = new Random();
            var isSuccess = random.Next(100) < 95;

            if (isSuccess)
            {
                return new TerminalPaymentResult
                {
                    Success = true,
                    TransactionId = transactionId,
                    Status = "authorized",
                    Amount = request.Amount,
                    ProcessedAt = DateTime.UtcNow,
                    ProviderData = new()
                    {
                        ["mock"] = true,
                        ["authCode"] = $"AUTH{random.Next(100000, 999999)}",
                        ["cardType"] = "VISA",
                        ["cardLast4"] = $"{random.Next(1000, 9999)}"
                    }
                };
            }
            else
            {
                return new TerminalPaymentResult
                {
                    Success = false,
                    TransactionId = transactionId,
                    Status = "declined",
                    ErrorCode = "INSUFFICIENT_FUNDS",
                    ErrorMessage = "Card declined - insufficient funds",
                    ProcessedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<TerminalPaymentResult> CapturePaymentAsync(string transactionId, decimal amount)
        {
            _logger.LogInformation(
                "Mock terminal: Capturing payment {TransactionId} for amount {Amount}",
                transactionId, amount);

            await Task.Delay(500);

            return new TerminalPaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                Status = "captured",
                Amount = amount,
                ProcessedAt = DateTime.UtcNow,
                ProviderData = new() { ["mock"] = true }
            };
        }

        public async Task<TerminalPaymentResult> RefundPaymentAsync(string transactionId, decimal amount, string? reason = null)
        {
            _logger.LogInformation(
                "Mock terminal: Refunding payment {TransactionId} for amount {Amount}. Reason: {Reason}",
                transactionId, amount, reason);

            await Task.Delay(1000);

            return new TerminalPaymentResult
            {
                Success = true,
                TransactionId = $"{transactionId}-REFUND",
                Status = "refunded",
                Amount = amount,
                ProcessedAt = DateTime.UtcNow,
                ProviderData = new() { ["mock"] = true, ["reason"] = reason ?? "No reason provided" }
            };
        }

        public async Task<TerminalPaymentResult> CancelPaymentAsync(string transactionId)
        {
            _logger.LogInformation("Mock terminal: Cancelling payment {TransactionId}", transactionId);

            await Task.Delay(300);

            return new TerminalPaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                Status = "cancelled",
                ProcessedAt = DateTime.UtcNow,
                ProviderData = new() { ["mock"] = true }
            };
        }

        public Task<TerminalPaymentStatus> GetPaymentStatusAsync(string transactionId)
        {
            _logger.LogInformation("Mock terminal: Checking status of payment {TransactionId}", transactionId);

            return Task.FromResult(new TerminalPaymentStatus
            {
                TransactionId = transactionId,
                Status = "captured",
                ProcessedAt = DateTime.UtcNow,
                ProviderData = new() { ["mock"] = true }
            });
        }

        public Task<bool> CheckTerminalStatusAsync()
        {
            _logger.LogInformation("Mock terminal: Checking terminal status");
            return Task.FromResult(true); // Always online in mock mode
        }
    }
}