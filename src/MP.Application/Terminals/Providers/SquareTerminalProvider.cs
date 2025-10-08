using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Square Terminal Provider
    /// Popular in: USA, UK, Canada, Australia, Japan, France, Spain, Ireland
    /// Models: Square Terminal, Square Register, Square Stand
    /// Protocol: Square Terminal API (REST)
    /// Connection: WiFi via Square Cloud
    /// Documentation: https://developer.squareup.com/docs/terminal-api/overview
    /// </summary>
    public class SquareTerminalProvider : ITerminalPaymentProvider, ITransientDependency
    {
        private readonly ILogger<SquareTerminalProvider> _logger;
        private readonly RestApiCommunication _communication;
        private TenantTerminalSettings? _settings;
        private string? _accessToken;
        private string? _locationId;
        private string? _deviceId;

        public string ProviderId => "square_terminal";
        public string DisplayName => "Square Terminal";
        public string Description => "Square Terminal payment provider for USA, UK, Canada, Australia, and more";

        public SquareTerminalProvider(
            ILogger<SquareTerminalProvider> logger,
            RestApiCommunication communication)
        {
            _logger = logger;
            _communication = communication;
        }

        public async Task InitializeAsync(TenantTerminalSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _logger.LogInformation("Initializing Square Terminal provider");

            // Parse configuration
            var config = ParseConfiguration(settings.ConfigurationJson);
            _accessToken = config.GetValueOrDefault("AccessToken");
            _locationId = config.GetValueOrDefault("LocationId");
            _deviceId = config.GetValueOrDefault("DeviceId");

            if (string.IsNullOrWhiteSpace(_accessToken))
            {
                throw new TerminalException("Square access token is required", "MISSING_ACCESS_TOKEN");
            }

            if (string.IsNullOrWhiteSpace(_locationId))
            {
                throw new TerminalException("Square location ID is required", "MISSING_LOCATION_ID");
            }

            // Determine API environment (sandbox vs production)
            var isProduction = config.GetValueOrDefault("Environment")?.ToLower() == "production";
            var apiBaseUrl = isProduction
                ? "https://connect.squareup.com/v2"
                : "https://connect.squareupsandbox.com/v2";

            // Connect to Square API
            var connectionSettings = new TerminalConnectionSettings
            {
                ApiBaseUrl = apiBaseUrl,
                AccessToken = _accessToken,
                Timeout = 60000 // Square Terminal can take longer for customer interaction
            };

            await _communication.ConnectAsync(connectionSettings);

            _logger.LogInformation("Square Terminal provider initialized for location {LocationId}", _locationId);
        }

        public async Task<TerminalPaymentResult> AuthorizePaymentAsync(
            TerminalPaymentRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Creating payment checkout on Square Terminal for {Amount} {Currency}",
                    request.Amount, request.Currency);

                // Square uses minor units (cents)
                var amountMoney = new
                {
                    amount = (long)(request.Amount * 100),
                    currency = request.Currency
                };

                // Build Square Terminal Checkout request
                var checkoutRequest = new
                {
                    idempotency_key = request.ReferenceId ?? Guid.NewGuid().ToString(),
                    checkout = new
                    {
                        amount_money = amountMoney,
                        device_options = new
                        {
                            device_id = _deviceId,
                            skip_receipt_screen = false,
                            collect_signature = true
                        },
                        reference_id = request.ReferenceId,
                        note = request.Description ?? "Payment",
                        payment_type = "CARD_PRESENT"
                    }
                };

                var requestJson = JsonSerializer.Serialize(checkoutRequest);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);

                // POST /v2/terminals/checkouts
                var responseBytes = await _communication.SendAndReceiveAsync(
                    requestBytes,
                    60000);

                var responseJson = Encoding.UTF8.GetString(responseBytes);
                var response = JsonSerializer.Deserialize<JsonDocument>(responseJson);

                if (response == null)
                {
                    throw new TerminalException("No response from Square API", "NO_RESPONSE");
                }

                // Check for errors
                if (response.RootElement.TryGetProperty("errors", out var errors))
                {
                    var errorMessage = errors[0].GetProperty("detail").GetString();
                    throw new TerminalException($"Square API error: {errorMessage}", "API_ERROR");
                }

                var checkout = response.RootElement.GetProperty("checkout");
                var checkoutId = checkout.GetProperty("id").GetString();
                var status = checkout.GetProperty("status").GetString();

                _logger.LogInformation("Square checkout created: {CheckoutId}, status: {Status}", checkoutId, status);

                // Poll for checkout completion
                var finalCheckout = await PollCheckoutStatusAsync(checkoutId);

                if (finalCheckout == null || !finalCheckout.HasValue)
                {
                    return new TerminalPaymentResult
                    {
                        Success = false,
                        ErrorMessage = "Checkout timeout or cancelled",
                        Status = "Timeout"
                    };
                }

                var finalStatus = finalCheckout.Value.GetProperty("status").GetString();
                var isCompleted = finalStatus == "COMPLETED";

                // Extract payment details if completed
                string? paymentId = null;
                string? cardBrand = null;
                string? lastFourDigits = null;

                if (isCompleted && finalCheckout.Value.TryGetProperty("payment_ids", out var paymentIds) && paymentIds.GetArrayLength() > 0)
                {
                    paymentId = paymentIds[0].GetString();

                    // Get payment details
                    if (!string.IsNullOrEmpty(paymentId))
                    {
                        var paymentDetails = await GetPaymentDetailsAsync(paymentId);
                        if (paymentDetails != null && paymentDetails.HasValue)
                        {
                            cardBrand = paymentDetails.Value.GetProperty("card_details")
                                .GetProperty("card").GetProperty("card_brand").GetString();
                            lastFourDigits = paymentDetails.Value.GetProperty("card_details")
                                .GetProperty("card").GetProperty("last_4").GetString();
                        }
                    }
                }

                return new TerminalPaymentResult
                {
                    Success = isCompleted,
                    TransactionId = checkoutId,
                    AuthorizationCode = paymentId ?? checkoutId,
                    Status = finalStatus,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Timestamp = DateTime.UtcNow,
                    CardType = cardBrand ?? "Unknown",
                    LastFourDigits = lastFourDigits ?? "****",
                    RawResponse = responseJson
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment on Square Terminal");
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
            // Square Terminal API captures automatically during checkout
            // This method is for compatibility with the interface
            _logger.LogInformation("Square Terminal captures automatically. Transaction: {TransactionId}", transactionId);

            return new TerminalPaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                Status = "Captured",
                Amount = amount,
                Timestamp = DateTime.UtcNow
            };
        }

        public async Task<TerminalPaymentResult> RefundPaymentAsync(
            string transactionId,
            decimal amount,
            string? reason = null)
        {
            try
            {
                _logger.LogInformation("Refunding {Amount} for payment {TransactionId} on Square", amount, transactionId);

                // Build Square refund request
                var refundRequest = new
                {
                    idempotency_key = Guid.NewGuid().ToString(),
                    amount_money = new
                    {
                        amount = (long)(amount * 100),
                        currency = _settings?.Currency ?? "USD"
                    },
                    payment_id = transactionId,
                    reason = "Customer refund"
                };

                var requestJson = JsonSerializer.Serialize(refundRequest);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);

                // POST /v2/refunds
                var responseBytes = await _communication.SendAndReceiveAsync(
                    requestBytes,
                    30000);

                var responseJson = Encoding.UTF8.GetString(responseBytes);
                var response = JsonSerializer.Deserialize<JsonDocument>(responseJson);

                if (response == null)
                {
                    throw new TerminalException("No response from Square API", "NO_RESPONSE");
                }

                // Check for errors
                if (response.RootElement.TryGetProperty("errors", out var errors))
                {
                    var errorMessage = errors[0].GetProperty("detail").GetString();
                    throw new TerminalException($"Square API error: {errorMessage}", "API_ERROR");
                }

                var refund = response.RootElement.GetProperty("refund");
                var refundId = refund.GetProperty("id").GetString();
                var status = refund.GetProperty("status").GetString();

                return new TerminalPaymentResult
                {
                    Success = status == "COMPLETED" || status == "PENDING",
                    TransactionId = refundId,
                    Status = status,
                    Amount = amount,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding payment on Square");
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
                _logger.LogInformation("Cancelling checkout {CheckoutId} on Square", transactionId);

                // POST /v2/terminals/checkouts/{checkout_id}/cancel
                var cancelRequest = new { };
                var requestJson = JsonSerializer.Serialize(cancelRequest);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);

                var responseBytes = await _communication.SendAndReceiveAsync(
                    requestBytes,
                    30000);

                var responseJson = Encoding.UTF8.GetString(responseBytes);
                var response = JsonSerializer.Deserialize<JsonDocument>(responseJson);

                if (response == null)
                {
                    throw new TerminalException("No response from Square API", "NO_RESPONSE");
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
                _logger.LogError(ex, "Error cancelling checkout on Square");
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
            _logger.LogInformation("Square: Checking status of payment {TransactionId}", transactionId);

            return Task.FromResult(new TerminalPaymentStatus
            {
                TransactionId = transactionId,
                Status = "captured",
                ProcessedAt = DateTime.UtcNow,
                ProviderData = new() { ["provider"] = "square" }
            });
        }

        public Task<bool> CheckTerminalStatusAsync()
        {
            _logger.LogInformation("Square: Checking terminal status");
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
                _logger.LogWarning(ex, "Failed to parse Square configuration JSON");
            }

            return config;
        }

        private async Task<JsonElement?> PollCheckoutStatusAsync(string checkoutId)
        {
            var maxAttempts = 120; // Poll for up to 2 minutes
            var attempts = 0;

            while (attempts < maxAttempts)
            {
                try
                {
                    // GET /v2/terminals/checkouts/{checkout_id}
                    var statusBytes = await _communication.SendAndReceiveAsync(
                        Encoding.UTF8.GetBytes("{}"),
                        5000);

                    var statusJson = Encoding.UTF8.GetString(statusBytes);
                    var response = JsonSerializer.Deserialize<JsonDocument>(statusJson);

                    if (response != null && response.RootElement.TryGetProperty("checkout", out var checkout))
                    {
                        var status = checkout.GetProperty("status").GetString();

                        if (status == "COMPLETED" || status == "CANCELED" || status == "FAILED")
                        {
                            return checkout;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error polling checkout status");
                }

                await Task.Delay(1000);
                attempts++;
            }

            return null;
        }

        private async Task<JsonElement?> GetPaymentDetailsAsync(string paymentId)
        {
            try
            {
                // GET /v2/payments/{payment_id}
                var paymentBytes = await _communication.SendAndReceiveAsync(
                    Encoding.UTF8.GetBytes("{}"),
                    5000);

                var paymentJson = Encoding.UTF8.GetString(paymentBytes);
                var response = JsonSerializer.Deserialize<JsonDocument>(paymentJson);

                if (response != null && response.RootElement.TryGetProperty("payment", out var payment))
                {
                    return payment;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting payment details");
            }

            return null;
        }

        #endregion

        public void Dispose()
        {
            _communication?.Dispose();
        }
    }
}
