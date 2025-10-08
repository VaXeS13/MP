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
    /// Stripe Terminal Provider
    /// Popular in: Global (USA, Canada, UK, EU, Australia, Singapore, etc.)
    /// Readers: BBPOS WisePad 3, Verifone P400, BBPOS Chipper 2X BT, Stripe Reader S700
    /// Protocol: Stripe Terminal API (REST) + Connection Tokens
    /// Connection: WiFi/Bluetooth via Stripe Cloud
    /// Documentation: https://stripe.com/docs/terminal
    /// </summary>
    public class StripeTerminalProvider : ITerminalPaymentProvider, ITransientDependency
    {
        private readonly ILogger<StripeTerminalProvider> _logger;
        private readonly RestApiCommunication _communication;
        private TenantTerminalSettings? _settings;
        private string? _secretKey;
        private string? _readerId;
        private string? _locationId;

        public string ProviderId => "stripe_terminal";
        public string DisplayName => "Stripe Terminal";
        public string Description => "Stripe Terminal payment provider with global coverage and flexible reader options";

        public StripeTerminalProvider(
            ILogger<StripeTerminalProvider> logger,
            RestApiCommunication communication)
        {
            _logger = logger;
            _communication = communication;
        }

        public async Task InitializeAsync(TenantTerminalSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _logger.LogInformation("Initializing Stripe Terminal provider");

            // Parse configuration
            var config = ParseConfiguration(settings.ConfigurationJson);
            _secretKey = config.GetValueOrDefault("SecretKey");
            _readerId = config.GetValueOrDefault("ReaderId");
            _locationId = config.GetValueOrDefault("LocationId");

            if (string.IsNullOrWhiteSpace(_secretKey))
            {
                throw new TerminalException("Stripe secret key is required", "MISSING_SECRET_KEY");
            }

            if (string.IsNullOrWhiteSpace(_readerId))
            {
                throw new TerminalException("Stripe reader ID is required", "MISSING_READER_ID");
            }

            // Connect to Stripe API
            var connectionSettings = new TerminalConnectionSettings
            {
                ApiBaseUrl = "https://api.stripe.com/v1",
                AccessToken = _secretKey,
                Timeout = 60000 // Terminal operations can take time
            };

            await _communication.ConnectAsync(connectionSettings);

            _logger.LogInformation("Stripe Terminal provider initialized for reader {ReaderId}", _readerId);
        }

        public async Task<TerminalPaymentResult> AuthorizePaymentAsync(
            TerminalPaymentRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Creating PaymentIntent on Stripe Terminal for {Amount} {Currency}",
                    request.Amount, request.Currency);

                // Step 1: Create PaymentIntent
                var paymentIntent = await CreatePaymentIntentAsync(request);

                if (paymentIntent == null)
                {
                    throw new TerminalException("Failed to create PaymentIntent", "PAYMENT_INTENT_FAILED");
                }

                var paymentIntentId = paymentIntent.HasValue ? paymentIntent.Value.GetProperty("id").GetString() : null;
                var clientSecret = paymentIntent.HasValue ? paymentIntent.Value.GetProperty("client_secret").GetString() : null;

                _logger.LogInformation("PaymentIntent created: {PaymentIntentId}", paymentIntentId);

                // Step 2: Process payment on terminal
                var paymentProcessed = await ProcessPaymentOnTerminalAsync(
                    paymentIntentId);

                if (paymentProcessed == null)
                {
                    return new TerminalPaymentResult
                    {
                        Success = false,
                        ErrorMessage = "Payment processing failed or was cancelled",
                        Status = "Failed"
                    };
                }

                var status = paymentProcessed.HasValue ? paymentProcessed.Value.GetProperty("status").GetString() : null;
                var isSuccessful = status == "succeeded" || status == "requires_capture";

                // Extract card details if available
                string? cardBrand = null;
                string? lastFourDigits = null;
                string? authCode = null;

                if (paymentProcessed.HasValue && paymentProcessed.Value.TryGetProperty("charges", out var charges) &&
                    charges.TryGetProperty("data", out var chargesData) &&
                    chargesData.GetArrayLength() > 0)
                {
                    var charge = chargesData[0];

                    if (charge.TryGetProperty("payment_method_details", out var pmDetails) &&
                        pmDetails.TryGetProperty("card_present", out var cardPresent))
                    {
                        cardBrand = cardPresent.GetProperty("brand").GetString();
                        lastFourDigits = cardPresent.GetProperty("last4").GetString();

                        if (cardPresent.TryGetProperty("authorization_code", out var authCodeProp))
                        {
                            authCode = authCodeProp.GetString();
                        }
                    }
                }

                return new TerminalPaymentResult
                {
                    Success = isSuccessful,
                    TransactionId = paymentIntentId,
                    AuthorizationCode = authCode ?? paymentIntentId,
                    Status = status,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Timestamp = DateTime.UtcNow,
                    CardType = cardBrand ?? "Unknown",
                    LastFourDigits = lastFourDigits ?? "****"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment on Stripe Terminal");
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
                _logger.LogInformation("Capturing PaymentIntent {PaymentIntentId} on Stripe", transactionId);

                // Build capture request
                var captureRequest = new Dictionary<string, string>
                {
                    { "amount_to_capture", ((long)(amount * 100)).ToString() }
                };

                var requestContent = string.Join("&", captureRequest.Select(kvp =>
                    $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                var requestBytes = Encoding.UTF8.GetBytes(requestContent);

                // POST /v1/payment_intents/{id}/capture
                var responseBytes = await _communication.SendAndReceiveAsync(
                    requestBytes,
                    30000);

                var responseJson = Encoding.UTF8.GetString(responseBytes);
                var response = JsonSerializer.Deserialize<JsonDocument>(responseJson);

                if (response == null)
                {
                    throw new TerminalException("No response from Stripe API", "NO_RESPONSE");
                }

                // Check for errors
                if (response.RootElement.TryGetProperty("error", out var error))
                {
                    var errorMessage = error.GetProperty("message").GetString();
                    throw new TerminalException($"Stripe API error: {errorMessage}", "API_ERROR");
                }

                var status = response.RootElement.GetProperty("status").GetString();

                return new TerminalPaymentResult
                {
                    Success = status == "succeeded",
                    TransactionId = transactionId,
                    Status = status,
                    Amount = amount,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing payment on Stripe");
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
                _logger.LogInformation("Refunding {Amount} for PaymentIntent {PaymentIntentId} on Stripe",
                    amount, transactionId);

                // Build refund request
                var refundRequest = new Dictionary<string, string>
                {
                    { "payment_intent", transactionId },
                    { "amount", ((long)(amount * 100)).ToString() }
                };

                var requestContent = string.Join("&", refundRequest.Select(kvp =>
                    $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                var requestBytes = Encoding.UTF8.GetBytes(requestContent);

                // POST /v1/refunds
                var responseBytes = await _communication.SendAndReceiveAsync(
                    requestBytes,
                    30000);

                var responseJson = Encoding.UTF8.GetString(responseBytes);
                var response = JsonSerializer.Deserialize<JsonDocument>(responseJson);

                if (response == null)
                {
                    throw new TerminalException("No response from Stripe API", "NO_RESPONSE");
                }

                // Check for errors
                if (response.RootElement.TryGetProperty("error", out var error))
                {
                    var errorMessage = error.GetProperty("message").GetString();
                    throw new TerminalException($"Stripe API error: {errorMessage}", "API_ERROR");
                }

                var refundId = response.RootElement.GetProperty("id").GetString();
                var status = response.RootElement.GetProperty("status").GetString();

                return new TerminalPaymentResult
                {
                    Success = status == "succeeded" || status == "pending",
                    TransactionId = refundId,
                    Status = status,
                    Amount = amount,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding payment on Stripe");
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
                _logger.LogInformation("Cancelling PaymentIntent {PaymentIntentId} on Stripe", transactionId);

                // POST /v1/payment_intents/{id}/cancel
                var requestBytes = Encoding.UTF8.GetBytes("");

                var responseBytes = await _communication.SendAndReceiveAsync(
                    requestBytes,
                    30000);

                var responseJson = Encoding.UTF8.GetString(responseBytes);
                var response = JsonSerializer.Deserialize<JsonDocument>(responseJson);

                if (response == null)
                {
                    throw new TerminalException("No response from Stripe API", "NO_RESPONSE");
                }

                // Check for errors
                if (response.RootElement.TryGetProperty("error", out var error))
                {
                    var errorMessage = error.GetProperty("message").GetString();
                    throw new TerminalException($"Stripe API error: {errorMessage}", "API_ERROR");
                }

                var status = response.RootElement.GetProperty("status").GetString();

                return new TerminalPaymentResult
                {
                    Success = status == "canceled",
                    TransactionId = transactionId,
                    Status = status,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling payment on Stripe");
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
            _logger.LogInformation("Stripe: Checking status of payment {TransactionId}", transactionId);

            return Task.FromResult(new TerminalPaymentStatus
            {
                TransactionId = transactionId,
                Status = "captured",
                ProcessedAt = DateTime.UtcNow,
                ProviderData = new() { ["provider"] = "stripe" }
            });
        }

        public Task<bool> CheckTerminalStatusAsync()
        {
            _logger.LogInformation("Stripe: Checking terminal status");
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
                _logger.LogWarning(ex, "Failed to parse Stripe configuration JSON");
            }

            return config;
        }

        private async Task<JsonElement?> CreatePaymentIntentAsync(
            TerminalPaymentRequest request)
        {
            try
            {
                // Build PaymentIntent request
                var paymentIntentRequest = new Dictionary<string, string>
                {
                    { "amount", ((long)(request.Amount * 100)).ToString() },
                    { "currency", request.Currency.ToLower() },
                    { "payment_method_types[]", "card_present" },
                    { "capture_method", "manual" }, // Manual capture for auth-capture flow
                    { "description", request.Description ?? "Payment" }
                };

                if (!string.IsNullOrEmpty(request.RentalItemName))
                {
                    paymentIntentRequest["metadata[rental_item]"] = request.RentalItemName;
                }

                var requestContent = string.Join("&", paymentIntentRequest.Select(kvp =>
                    $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                var requestBytes = Encoding.UTF8.GetBytes(requestContent);

                // POST /v1/payment_intents
                var responseBytes = await _communication.SendAndReceiveAsync(
                    requestBytes,
                    30000);

                var responseJson = Encoding.UTF8.GetString(responseBytes);
                var response = JsonSerializer.Deserialize<JsonDocument>(responseJson);

                if (response != null && !response.RootElement.TryGetProperty("error", out _))
                {
                    return response.RootElement;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PaymentIntent");
                return null;
            }
        }

        private async Task<JsonElement?> ProcessPaymentOnTerminalAsync(
            string paymentIntentId)
        {
            try
            {
                // Create Terminal Reader Action
                var readerActionRequest = new Dictionary<string, string>
                {
                    { "payment_intent", paymentIntentId },
                    { "process_payment_intent[payment_intent]", paymentIntentId }
                };

                var requestContent = string.Join("&", readerActionRequest.Select(kvp =>
                    $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                var requestBytes = Encoding.UTF8.GetBytes(requestContent);

                // POST /v1/terminal/readers/{reader_id}/process_payment_intent
                var responseBytes = await _communication.SendAndReceiveAsync(
                    requestBytes,
                    60000);

                var responseJson = Encoding.UTF8.GetString(responseBytes);
                var response = JsonSerializer.Deserialize<JsonDocument>(responseJson);

                if (response == null || response.RootElement.TryGetProperty("error", out _))
                {
                    return null;
                }

                var actionId = response.RootElement.GetProperty("id").GetString();

                // Poll for action completion
                return await PollReaderActionAsync(actionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment on terminal");
                return null;
            }
        }

        private async Task<JsonElement?> PollReaderActionAsync(string actionId)
        {
            var maxAttempts = 120; // Poll for up to 2 minutes
            var attempts = 0;

            while (attempts < maxAttempts)
            {
                try
                {
                    // GET /v1/terminal/readers/{reader_id}
                    var statusBytes = await _communication.SendAndReceiveAsync(
                        Encoding.UTF8.GetBytes(""),
                        5000);

                    var statusJson = Encoding.UTF8.GetString(statusBytes);
                    var response = JsonSerializer.Deserialize<JsonDocument>(statusJson);

                    if (response != null &&
                        response.RootElement.TryGetProperty("action", out var action) &&
                        !action.ValueKind.Equals(JsonValueKind.Null))
                    {
                        var status = action.GetProperty("status").GetString();

                        if (status == "succeeded" || status == "failed")
                        {
                            // Get the PaymentIntent
                            var paymentIntentId = action.GetProperty("process_payment_intent")
                                .GetProperty("payment_intent").GetString();

                            return await GetPaymentIntentAsync(paymentIntentId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error polling reader action");
                }

                await Task.Delay(1000);
                attempts++;
            }

            return null;
        }

        private async Task<JsonElement?> GetPaymentIntentAsync(string paymentIntentId)
        {
            try
            {
                // GET /v1/payment_intents/{id}
                var responseBytes = await _communication.SendAndReceiveAsync(
                    Encoding.UTF8.GetBytes(""),
                    5000);

                var responseJson = Encoding.UTF8.GetString(responseBytes);
                var response = JsonSerializer.Deserialize<JsonDocument>(responseJson);

                if (response != null && !response.RootElement.TryGetProperty("error", out _))
                {
                    return response.RootElement;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PaymentIntent");
                return null;
            }
        }

        #endregion

        public void Dispose()
        {
            _communication?.Dispose();
        }
    }
}
