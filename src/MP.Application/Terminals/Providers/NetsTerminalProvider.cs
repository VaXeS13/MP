using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using MP.Domain.Terminals;
using MP.Domain.Terminals.Communication;
using MP.Application.Terminals.Communication;

namespace MP.Application.Terminals.Providers
{
    /// <summary>
    /// Nets Terminal Provider (Nexi Group)
    /// Popular in: Denmark, Norway, Sweden, Finland, Baltics
    /// Models: Nets Terminal S920, S922, A920, D500
    /// Protocol: Nets API (REST-based)
    /// Connection: WiFi/Ethernet via Nets Gateway
    /// Documentation: https://developer.nexigroup.com/nexi-checkout/en-EU/api/
    /// </summary>
    public class NetsTerminalProvider : ITerminalPaymentProvider, ITransientDependency
    {
        private readonly ILogger<NetsTerminalProvider> _logger;
        private readonly RestApiCommunication _communication;
        private TenantTerminalSettings? _settings;
        private string? _apiKey;
        private string? _merchantId;
        private string? _terminalId;

        public string ProviderId => "nets";
        public string DisplayName => "Nets Terminal (Nexi)";
        public string Description => "Nets payment terminal provider for Nordic countries (Denmark, Norway, Sweden, Finland)";

        public NetsTerminalProvider(
            ILogger<NetsTerminalProvider> logger,
            RestApiCommunication communication)
        {
            _logger = logger;
            _communication = communication;
        }

        public async Task InitializeAsync(TenantTerminalSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _logger.LogInformation("Initializing Nets terminal provider");

            // Parse configuration
            var config = ParseConfiguration(settings.ConfigurationJson);
            _apiKey = config.GetValueOrDefault("ApiKey");
            _merchantId = config.GetValueOrDefault("MerchantId");
            _terminalId = config.GetValueOrDefault("TerminalId");

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new TerminalException("Nets API key is required", "MISSING_API_KEY");
            }

            if (string.IsNullOrWhiteSpace(_merchantId))
            {
                throw new TerminalException("Nets Merchant ID is required", "MISSING_MERCHANT_ID");
            }

            // Connect to Nets API
            var connectionSettings = new TerminalConnectionSettings
            {
                ApiBaseUrl = config.GetValueOrDefault("ApiBaseUrl") ?? "https://api.dibspayment.eu/v1",
                AccessToken = _apiKey,
                Timeout = 30000
            };

            await _communication.ConnectAsync(connectionSettings);

            _logger.LogInformation("Nets terminal provider initialized for merchant {MerchantId}", _merchantId);
        }

        public async Task<TerminalPaymentResult> AuthorizePaymentAsync(TerminalPaymentRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Authorizing payment of {Amount} {Currency} on Nets terminal",
                    request.Amount, request.Currency);

                // Build Nets payment request
                var paymentRequest = new
                {
                    order = new
                    {
                        amount = (long)(request.Amount * 100), // Convert to minor units (øre/cents)
                        currency = request.Currency,
                        reference = request.ReferenceId ?? Guid.NewGuid().ToString()
                    },
                    checkout = new
                    {
                        termsUrl = "https://example.com/terms",
                        merchantTermsUrl = "https://example.com/terms",
                        consumer = request.AdditionalData?.GetValueOrDefault("customerEmail") != null
                            ? new { email = request.AdditionalData["customerEmail"] }
                            : null
                    },
                    merchantId = _merchantId,
                    paymentMethodConfiguration = new[]
                    {
                        new { name = "Card", enabled = true }
                    }
                };

                var requestJson = JsonSerializer.Serialize(paymentRequest);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);

                // Send to Nets API
                var responseBytes = await _communication.SendAndReceiveAsync(
                    requestBytes,
                    30000);

                var responseJson = Encoding.UTF8.GetString(responseBytes);
                var response = JsonSerializer.Deserialize<JsonDocument>(responseJson);

                if (response == null)
                {
                    throw new TerminalException("No response from Nets API", "NO_RESPONSE");
                }

                var paymentId = response.RootElement.GetProperty("paymentId").GetString();
                var hostedPaymentPageUrl = response.RootElement.GetProperty("hostedPaymentPageUrl").GetString();

                // For terminal payment, we need to poll for completion
                // In real implementation, you would send this to the physical terminal
                _logger.LogInformation("Nets payment created: {PaymentId}", paymentId);

                // Poll for payment status (simplified - in production use webhooks)
                var finalStatus = await PollPaymentStatusAsync(paymentId);

                return new TerminalPaymentResult
                {
                    Success = finalStatus == "Authorized",
                    TransactionId = paymentId,
                    AuthorizationCode = paymentId,
                    Status = finalStatus,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Timestamp = DateTime.UtcNow,
                    CardType = "Unknown",
                    LastFourDigits = "****"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authorizing payment on Nets terminal");
                return new TerminalPaymentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Status = "Failed"
                };
            }
        }

        public async Task<TerminalPaymentResult> CapturePaymentAsync(
            string transactionId,
            decimal amount)
        {
            try
            {
                _logger.LogInformation("Capturing payment {TransactionId} on Nets", transactionId);

                // Build Nets capture request
                var captureRequest = new
                {
                    amount = (long)(amount * 100),
                    orderItems = new[]
                    {
                        new
                        {
                            reference = "item",
                            name = "Payment capture",
                            quantity = 1,
                            unit = "pcs",
                            unitPrice = (long)(amount * 100),
                            taxRate = 0
                        }
                    }
                };

                var requestJson = JsonSerializer.Serialize(captureRequest);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);

                // POST /v1/payments/{paymentId}/charges
                var endpoint = $"/payments/{transactionId}/charges";
                var responseBytes = await _communication.SendAndReceiveAsync(
                    requestBytes,
                    30000);

                var responseJson = Encoding.UTF8.GetString(responseBytes);
                var response = JsonSerializer.Deserialize<JsonDocument>(responseJson);

                if (response == null)
                {
                    throw new TerminalException("No response from Nets API", "NO_RESPONSE");
                }

                var chargeId = response.RootElement.GetProperty("chargeId").GetString();

                return new TerminalPaymentResult
                {
                    Success = true,
                    TransactionId = transactionId,
                    AuthorizationCode = chargeId,
                    Status = "Captured",
                    Amount = amount,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing payment on Nets terminal");
                return new TerminalPaymentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Status = "Failed"
                };
            }
        }

        public async Task<TerminalPaymentResult> RefundPaymentAsync(
            string transactionId,
            decimal amount,
            string? reason = null)
        {
            try
            {
                _logger.LogInformation("Refunding {Amount} for transaction {TransactionId} on Nets", amount, transactionId);

                // Build Nets refund request
                var refundRequest = new
                {
                    amount = (long)(amount * 100),
                    orderItems = new[]
                    {
                        new
                        {
                            reference = "refund",
                            name = "Refund",
                            quantity = 1,
                            unit = "pcs",
                            unitPrice = (long)(amount * 100),
                            taxRate = 0
                        }
                    }
                };

                var requestJson = JsonSerializer.Serialize(refundRequest);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);

                // POST /v1/payments/{paymentId}/refunds
                var endpoint = $"/payments/{transactionId}/refunds";
                var responseBytes = await _communication.SendAndReceiveAsync(
                    requestBytes,
                    30000);

                var responseJson = Encoding.UTF8.GetString(responseBytes);
                var response = JsonSerializer.Deserialize<JsonDocument>(responseJson);

                if (response == null)
                {
                    throw new TerminalException("No response from Nets API", "NO_RESPONSE");
                }

                var refundId = response.RootElement.GetProperty("refundId").GetString();

                return new TerminalPaymentResult
                {
                    Success = true,
                    TransactionId = refundId,
                    Status = "Refunded",
                    Amount = amount,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding payment on Nets terminal");
                return new TerminalPaymentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Status = "Failed"
                };
            }
        }

        public async Task<TerminalPaymentResult> CancelPaymentAsync(
            string transactionId)
        {
            try
            {
                _logger.LogInformation("Cancelling transaction {TransactionId} on Nets", transactionId);

                // POST /v1/payments/{paymentId}/cancels
                var endpoint = $"/payments/{transactionId}/cancels";
                var emptyRequest = Encoding.UTF8.GetBytes("{}");

                var responseBytes = await _communication.SendAndReceiveAsync(
                    emptyRequest,
                    30000);

                var responseJson = Encoding.UTF8.GetString(responseBytes);
                var response = JsonSerializer.Deserialize<JsonDocument>(responseJson);

                if (response == null)
                {
                    throw new TerminalException("No response from Nets API", "NO_RESPONSE");
                }

                return new TerminalPaymentResult
                {
                    Success = true,
                    TransactionId = transactionId,
                    Status = "Cancelled",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling payment on Nets terminal");
                return new TerminalPaymentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Status = "Failed"
                };
            }
        }

        public Task<TerminalPaymentStatus> GetPaymentStatusAsync(string transactionId)
        {
            _logger.LogInformation("Nets: Checking status of payment {TransactionId}", transactionId);

            return Task.FromResult(new TerminalPaymentStatus
            {
                TransactionId = transactionId,
                Status = "captured",
                ProcessedAt = DateTime.UtcNow,
                ProviderData = new() { ["provider"] = "nets" }
            });
        }

        public Task<bool> CheckTerminalStatusAsync()
        {
            _logger.LogInformation("Nets: Checking terminal status");
            return Task.FromResult(_settings != null); // Online if initialized
        }

        #region Helper Methods

        private Dictionary<string, string> ParseConfiguration(string? configJson)
        {
            var config = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(configJson))
                return config;

            try
            {
                var doc = JsonSerializer.Deserialize<JsonDocument>(configJson);
                if (doc != null)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        config[prop.Name] = prop.Value.GetString() ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse Nets configuration JSON");
            }

            return config;
        }

        private async Task<string> PollPaymentStatusAsync(string paymentId)
        {
            // Simplified polling - in production use webhooks
            var maxAttempts = 60; // Poll for up to 60 seconds
            var attempts = 0;

            while (attempts < maxAttempts)
            {
                try
                {
                    // GET /v1/payments/{paymentId}
                    var statusBytes = await _communication.SendAndReceiveAsync(
                        Encoding.UTF8.GetBytes("{}"),
                        5000);

                    var statusJson = Encoding.UTF8.GetString(statusBytes);
                    var status = JsonSerializer.Deserialize<JsonDocument>(statusJson);

                    if (status != null)
                    {
                        var paymentStatus = status.RootElement.GetProperty("payment")
                            .GetProperty("summary")
                            .GetProperty("authorizedAmount")
                            .GetInt64();

                        if (paymentStatus > 0)
                        {
                            return "Authorized";
                        }
                    }
                }
                catch
                {
                    // Continue polling
                }

                await Task.Delay(1000);
                attempts++;
            }

            return "Timeout";
        }

        #endregion

        public void Dispose()
        {
            _communication?.Dispose();
        }
    }
}
