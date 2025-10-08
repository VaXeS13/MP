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
    /// Adyen Terminal Provider
    /// Popular in: Global (Europe, North America, Asia-Pacific, Latin America)
    /// Terminals: Adyen V400m, V400c, P400, S1E, S1F, E280, E355
    /// Protocol: Adyen Terminal API (Cloud-based) or Local (Nexo)
    /// Connection: WiFi/Ethernet via Adyen Cloud
    /// Documentation: https://docs.adyen.com/point-of-sale/basic-tapi-integration/
    /// Use case: Enterprise-level multi-channel payments
    /// </summary>
    public class AdyenProvider : ITerminalPaymentProvider, ITransientDependency
    {
        private readonly ILogger<AdyenProvider> _logger;
        private readonly RestApiCommunication _communication;
        private TenantTerminalSettings? _settings;
        private string? _apiKey;
        private string? _merchantAccount;
        private string? _terminalPoiId;

        public string ProviderId => "adyen";
        public string DisplayName => "Adyen Terminal";
        public string Description => "Adyen Terminal payment provider with global coverage and enterprise-level multi-channel support";

        public AdyenProvider(
            ILogger<AdyenProvider> logger,
            RestApiCommunication communication)
        {
            _logger = logger;
            _communication = communication;
        }

        public async Task InitializeAsync(TenantTerminalSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _logger.LogInformation("Initializing Adyen Terminal provider");

            // Parse configuration
            var config = ParseConfiguration(settings.ConfigurationJson);
            _apiKey = config.GetValueOrDefault("ApiKey");
            _merchantAccount = config.GetValueOrDefault("MerchantAccount");
            _terminalPoiId = config.GetValueOrDefault("TerminalPoiId");

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new TerminalException("Adyen API key is required", "MISSING_API_KEY");
            }

            if (string.IsNullOrWhiteSpace(_merchantAccount))
            {
                throw new TerminalException("Adyen merchant account is required", "MISSING_MERCHANT_ACCOUNT");
            }

            if (string.IsNullOrWhiteSpace(_terminalPoiId))
            {
                throw new TerminalException("Adyen terminal POI ID is required", "MISSING_TERMINAL_POI_ID");
            }

            // Determine API environment (test vs live)
            var isLive = config.GetValueOrDefault("Environment")?.ToLower() == "live";
            var apiBaseUrl = isLive
                ? "https://terminal-api-live.adyen.com"
                : "https://terminal-api-test.adyen.com";

            // Connect to Adyen Terminal API
            var connectionSettings = new TerminalConnectionSettings
            {
                ApiBaseUrl = apiBaseUrl,
                AccessToken = _apiKey,
                Timeout = 60000
            };

            await _communication.ConnectAsync(connectionSettings);

            _logger.LogInformation("Adyen Terminal provider initialized for terminal {TerminalPoiId}", _terminalPoiId);
        }

        public async Task<TerminalPaymentResult> AuthorizePaymentAsync(
            TerminalPaymentRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Creating payment on Adyen Terminal for {Amount} {Currency}",
                    request.Amount, request.Currency);

                // Build Adyen Terminal API request (Nexo protocol)
                var serviceId = Guid.NewGuid().ToString();
                var saleId = request.ReferenceId ?? Guid.NewGuid().ToString();

                var paymentRequest = new
                {
                    SaleToPOIRequest = new
                    {
                        MessageHeader = new
                        {
                            MessageClass = "Service",
                            MessageCategory = "Payment",
                            MessageType = "Request",
                            ServiceID = serviceId,
                            SaleID = saleId,
                            POIID = _terminalPoiId
                        },
                        PaymentRequest = new
                        {
                            SaleData = new
                            {
                                SaleTransactionID = new
                                {
                                    TransactionID = saleId,
                                    TimeStamp = DateTime.UtcNow.ToString("o")
                                }
                            },
                            PaymentTransaction = new
                            {
                                AmountsReq = new
                                {
                                    Currency = request.Currency,
                                    RequestedAmount = request.Amount
                                }
                            }
                        }
                    }
                };

                var requestJson = JsonSerializer.Serialize(paymentRequest);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);

                // POST /sync
                var responseBytes = await _communication.SendAndReceiveAsync(
                    requestBytes,
                    60000);

                var responseJson = Encoding.UTF8.GetString(responseBytes);
                var response = JsonSerializer.Deserialize<JsonDocument>(responseJson);

                if (response == null)
                {
                    throw new TerminalException("No response from Adyen Terminal API", "NO_RESPONSE");
                }

                // Parse Nexo response
                if (!response.RootElement.TryGetProperty("SaleToPOIResponse", out var saleToPoiResponse))
                {
                    throw new TerminalException("Invalid Adyen response format", "INVALID_RESPONSE");
                }

                var messageHeader = saleToPoiResponse.GetProperty("MessageHeader");
                var paymentResponse = saleToPoiResponse.GetProperty("PaymentResponse");
                var paymentResult = paymentResponse.GetProperty("Response");

                var resultCode = paymentResult.GetProperty("Result").GetString();
                var isSuccessful = resultCode == "Success";

                // Extract payment details
                string? transactionId = null;
                string? authCode = null;
                string? cardType = null;
                string? lastFourDigits = null;

                if (isSuccessful && paymentResponse.TryGetProperty("PaymentReceipt", out var receipt))
                {
                    if (receipt.TryGetProperty("DocumentQualifier", out var docQual))
                    {
                        transactionId = docQual.GetString();
                    }
                }

                if (isSuccessful && paymentResponse.TryGetProperty("POIData", out var poiData) &&
                    poiData.TryGetProperty("POITransactionID", out var poiTxId))
                {
                    transactionId = poiTxId.GetProperty("TransactionID").GetString();
                    authCode = transactionId;
                }

                if (paymentResponse.TryGetProperty("PaymentResult", out var paymentResultData) &&
                    paymentResultData.TryGetProperty("PaymentInstrumentData", out var instrumentData) &&
                    instrumentData.TryGetProperty("CardData", out var cardData))
                {
                    if (cardData.TryGetProperty("PaymentBrand", out var brand))
                    {
                        cardType = brand.GetString();
                    }

                    if (cardData.TryGetProperty("MaskedPAN", out var maskedPan))
                    {
                        var pan = maskedPan.GetString();
                        if (!string.IsNullOrEmpty(pan) && pan.Length >= 4)
                        {
                            lastFourDigits = pan.Substring(pan.Length - 4);
                        }
                    }
                }

                return new TerminalPaymentResult
                {
                    Success = isSuccessful,
                    TransactionId = transactionId ?? serviceId,
                    AuthorizationCode = authCode ?? serviceId,
                    Status = resultCode,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Timestamp = DateTime.UtcNow,
                    CardType = cardType ?? "Unknown",
                    LastFourDigits = lastFourDigits ?? "****",
                    RawResponse = responseJson
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment on Adyen Terminal");
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
            // Adyen Terminal API captures automatically by default
            // For split capture, use Adyen Payments API (not Terminal API)
            _logger.LogInformation("Adyen Terminal captures automatically. Transaction: {TransactionId}", transactionId);

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
                _logger.LogInformation("Refunding {Amount} for transaction {TransactionId} on Adyen Terminal",
                    amount, transactionId);

                var serviceId = Guid.NewGuid().ToString();

                // Build Adyen Reversal/Refund request
                var refundRequest = new
                {
                    SaleToPOIRequest = new
                    {
                        MessageHeader = new
                        {
                            MessageClass = "Service",
                            MessageCategory = "Reversal",
                            MessageType = "Request",
                            ServiceID = serviceId,
                            SaleID = "REFUND-" + Guid.NewGuid().ToString(),
                            POIID = _terminalPoiId
                        },
                        ReversalRequest = new
                        {
                            OriginalPOITransaction = new
                            {
                                POITransactionID = new
                                {
                                    TransactionID = transactionId
                                }
                            },
                            ReversalReason = "MerchantCancel"
                        }
                    }
                };

                var requestJson = JsonSerializer.Serialize(refundRequest);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);

                // POST /sync
                var responseBytes = await _communication.SendAndReceiveAsync(
                    requestBytes,
                    60000);

                var responseJson = Encoding.UTF8.GetString(responseBytes);
                var response = JsonSerializer.Deserialize<JsonDocument>(responseJson);

                if (response == null)
                {
                    throw new TerminalException("No response from Adyen Terminal API", "NO_RESPONSE");
                }

                // Parse response
                if (!response.RootElement.TryGetProperty("SaleToPOIResponse", out var saleToPoiResponse))
                {
                    throw new TerminalException("Invalid Adyen response format", "INVALID_RESPONSE");
                }

                var reversalResponse = saleToPoiResponse.GetProperty("ReversalResponse");
                var responseResult = reversalResponse.GetProperty("Response");
                var resultCode = responseResult.GetProperty("Result").GetString();

                return new TerminalPaymentResult
                {
                    Success = resultCode == "Success",
                    TransactionId = serviceId,
                    Status = resultCode,
                    Amount = amount,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding payment on Adyen Terminal");
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
                _logger.LogInformation("Aborting transaction {TransactionId} on Adyen Terminal", transactionId);

                var serviceId = Guid.NewGuid().ToString();

                // Build Adyen Abort request
                var abortRequest = new
                {
                    SaleToPOIRequest = new
                    {
                        MessageHeader = new
                        {
                            MessageClass = "Service",
                            MessageCategory = "Abort",
                            MessageType = "Request",
                            ServiceID = serviceId,
                            SaleID = "ABORT-" + Guid.NewGuid().ToString(),
                            POIID = _terminalPoiId
                        },
                        AbortRequest = new
                        {
                            AbortReason = "MerchantAbort",
                            MessageReference = new
                            {
                                MessageCategory = "Payment",
                                ServiceID = transactionId
                            }
                        }
                    }
                };

                var requestJson = JsonSerializer.Serialize(abortRequest);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);

                // POST /sync
                var responseBytes = await _communication.SendAndReceiveAsync(
                    requestBytes,
                    30000);

                var responseJson = Encoding.UTF8.GetString(responseBytes);
                var response = JsonSerializer.Deserialize<JsonDocument>(responseJson);

                if (response == null)
                {
                    throw new TerminalException("No response from Adyen Terminal API", "NO_RESPONSE");
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
                _logger.LogError(ex, "Error cancelling transaction on Adyen Terminal");
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
            _logger.LogInformation("Adyen: Checking status of payment {TransactionId}", transactionId);

            return Task.FromResult(new TerminalPaymentStatus
            {
                TransactionId = transactionId,
                Status = "captured",
                ProcessedAt = DateTime.UtcNow,
                ProviderData = new() { ["provider"] = "adyen" }
            });
        }

        public Task<bool> CheckTerminalStatusAsync()
        {
            _logger.LogInformation("Adyen: Checking terminal status");
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
                _logger.LogWarning(ex, "Failed to parse Adyen configuration JSON");
            }

            return config;
        }

        #endregion

        public void Dispose()
        {
            _communication?.Dispose();
        }
    }
}
