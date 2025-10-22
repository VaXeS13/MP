using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;
using MP.Domain.Payments;
using MP.Domain.Settings;
using Stripe;
using Stripe.Checkout;

namespace MP.Application.Payments
{
    /// <summary>
    /// Stripe implementation of IPaymentProvider
    /// Supports credit/debit cards and digital wallets
    /// </summary>
    public class StripeProvider : IPaymentProvider
    {
        private readonly ISettingProvider _settingProvider;
        private readonly ILogger<StripeProvider> _logger;
        private readonly ICurrentTenant _currentTenant;

        public string ProviderId => "Stripe";
        public string DisplayName => "Stripe";
        public string Description => "Accept credit cards, debit cards, and digital wallets with Stripe";
        public string? LogoUrl => "https://stripe.com/img/v3/logos/logo-stripe.png";
        public List<string> SupportedCurrencies => new() { "PLN", "EUR", "USD", "GBP", "CHF", "SEK", "NOK", "DKK" };

        public StripeProvider(
            ISettingProvider settingProvider,
            ILogger<StripeProvider> logger,
            ICurrentTenant currentTenant)
        {
            _settingProvider = settingProvider;
            _logger = logger;
            _currentTenant = currentTenant;
        }

        public bool IsActive
        {
            get
            {
                try
                {
                    var isEnabled = _settingProvider.GetAsync<bool>(MPSettings.PaymentProviders.StripeEnabled).Result;
                    return isEnabled;
                }
                catch
                {
                    return false;
                }
            }
        }

        public async Task<PaymentResult> CreatePaymentAsync(PaymentRequest request)
        {
            try
            {
                _logger.LogInformation("StripeProvider: Creating payment for amount {Amount} {Currency}",
                    request.Amount, request.Currency);

                var isEnabled = await _settingProvider.GetAsync<bool>(MPSettings.PaymentProviders.StripeEnabled);
                if (!isEnabled)
                {
                    return new PaymentResult
                    {
                        Success = false,
                        ErrorMessage = "Stripe provider is not configured or disabled",
                        TransactionId = string.Empty,
                        PaymentUrl = string.Empty
                    };
                }

                var publishableKey = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.StripePublishableKey);
                var secretKey = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.StripeSecretKey);
                var webhookSecret = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.StripeWebhookSecret);

                if (string.IsNullOrEmpty(secretKey))
                {
                    return new PaymentResult
                    {
                        Success = false,
                        ErrorMessage = "Stripe configuration is incomplete",
                        TransactionId = string.Empty,
                        PaymentUrl = string.Empty
                    };
                }

                // Configure Stripe API key
                StripeConfiguration.ApiKey = secretKey;

                // Determine payment method types based on request
                var paymentMethodTypes = new List<string> { "card" };

                // Add Google Pay and Apple Pay if available
                if (string.IsNullOrEmpty(request.MethodId) || request.MethodId == "google_pay")
                {
                    // Google Pay and Apple Pay are automatically shown when card is enabled
                    // and the customer's device supports them
                }
                else if (request.MethodId == "card")
                {
                    // Just cards
                    paymentMethodTypes = new List<string> { "card" };
                }

                // Create Stripe Checkout Session
                _logger.LogInformation("StripeProvider: Preparing Checkout Session with SuccessUrl: {SuccessUrl}",
                    request.UrlReturn + "?session_id={CHECKOUT_SESSION_ID}");

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = paymentMethodTypes,
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = request.Currency.ToLowerInvariant(),
                                UnitAmount = (long)(request.Amount * 100), // Stripe uses cents/smallest currency unit
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = request.Description,
                                    Description = $"Payment for {request.ClientName}"
                                }
                            },
                            Quantity = 1
                        }
                    },
                    Mode = "payment",
                    SuccessUrl = request.UrlReturn + "?session_id={CHECKOUT_SESSION_ID}",
                    CancelUrl = request.UrlReturn + "?cancelled=true",
                    CustomerEmail = request.Email,
                    ClientReferenceId = request.SessionId,
                    Metadata = new Dictionary<string, string>
                    {
                        { "merchant_id", request.MerchantId },
                        { "session_id", request.SessionId },
                        { "client_name", request.ClientName },
                        { "tenant_id", _currentTenant.Id?.ToString() ?? "host" }
                    },
                    PaymentIntentData = new SessionPaymentIntentDataOptions
                    {
                        Metadata = new Dictionary<string, string>
                        {
                            { "merchant_id", request.MerchantId },
                            { "session_id", request.SessionId }
                        }
                    }
                };

                // Add additional metadata from request
                foreach (var kvp in request.Metadata)
                {
                    if (!options.Metadata.ContainsKey(kvp.Key))
                    {
                        options.Metadata[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
                    }
                }

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                _logger.LogInformation("StripeProvider: Created Stripe Checkout Session {SessionId}", session.Id);

                return new PaymentResult
                {
                    TransactionId = session.Id,
                    PaymentUrl = session.Url,
                    Success = true,
                    ProviderData = new Dictionary<string, object>
                    {
                        { "stripe_session_id", session.Id },
                        { "stripe_payment_intent_id", session.PaymentIntentId ?? string.Empty },
                        { "stripe_amount", (long)(request.Amount * 100) },
                        { "stripe_currency", request.Currency.ToLowerInvariant() },
                        { "return_url", request.UrlReturn },
                        { "cancel_url", request.UrlReturn }
                    }
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "StripeProvider: Stripe API error creating payment. Code: {Code}, Type: {Type}",
                    ex.StripeError?.Code, ex.StripeError?.Type);
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = $"Payment creation failed: {ex.StripeError?.Message ?? ex.Message}",
                    TransactionId = string.Empty,
                    PaymentUrl = string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StripeProvider: Error creating payment");
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = "Payment creation failed",
                    TransactionId = string.Empty,
                    PaymentUrl = string.Empty
                };
            }
        }

        public async Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionId)
        {
            try
            {
                _logger.LogInformation("StripeProvider: Getting payment status for {TransactionId}", transactionId);

                var secretKey = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.StripeSecretKey);
                if (string.IsNullOrEmpty(secretKey))
                {
                    return new PaymentStatusResult
                    {
                        TransactionId = transactionId,
                        Status = "error",
                        ErrorMessage = "Stripe is not configured"
                    };
                }

                StripeConfiguration.ApiKey = secretKey;

                // Check if transactionId is a Session ID or Payment Intent ID
                if (transactionId.StartsWith("cs_"))
                {
                    // It's a Checkout Session ID
                    var sessionService = new SessionService();
                    var session = await sessionService.GetAsync(transactionId);

                    var status = session.PaymentStatus switch
                    {
                        "paid" => "completed",
                        "unpaid" => "pending",
                        "no_payment_required" => "completed",
                        _ => "pending"
                    };

                    return new PaymentStatusResult
                    {
                        TransactionId = transactionId,
                        Status = status,
                        Amount = session.AmountTotal.HasValue ? session.AmountTotal.Value / 100m : null,
                        CompletedAt = session.PaymentStatus == "paid" ? session.Created : null,
                        ProviderData = new Dictionary<string, object>
                        {
                            { "stripe_session_id", session.Id },
                            { "stripe_payment_intent_id", session.PaymentIntentId ?? string.Empty },
                            { "stripe_payment_status", session.PaymentStatus },
                            { "stripe_status", session.Status }
                        }
                    };
                }
                else if (transactionId.StartsWith("pi_"))
                {
                    // It's a Payment Intent ID
                    var piService = new PaymentIntentService();
                    var paymentIntent = await piService.GetAsync(transactionId);

                    var status = paymentIntent.Status switch
                    {
                        "succeeded" => "completed",
                        "processing" => "pending",
                        "requires_payment_method" => "pending",
                        "requires_confirmation" => "pending",
                        "requires_action" => "pending",
                        "canceled" => "cancelled",
                        "failed" => "failed",
                        _ => "pending"
                    };

                    return new PaymentStatusResult
                    {
                        TransactionId = transactionId,
                        Status = status,
                        Amount = paymentIntent.Amount / 100m,
                        CompletedAt = paymentIntent.Status == "succeeded" ? paymentIntent.Created : null,
                        ErrorCode = paymentIntent.LastPaymentError?.Code,
                        ErrorMessage = paymentIntent.LastPaymentError?.Message,
                        ProviderData = new Dictionary<string, object>
                        {
                            { "stripe_payment_intent_id", paymentIntent.Id },
                            { "stripe_status", paymentIntent.Status },
                            { "stripe_amount", paymentIntent.Amount },
                            { "stripe_currency", paymentIntent.Currency }
                        }
                    };
                }
                else
                {
                    return new PaymentStatusResult
                    {
                        TransactionId = transactionId,
                        Status = "error",
                        ErrorMessage = "Invalid transaction ID format"
                    };
                }
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "StripeProvider: Stripe API error getting payment status for {TransactionId}. Code: {Code}",
                    transactionId, ex.StripeError?.Code);
                return new PaymentStatusResult
                {
                    TransactionId = transactionId,
                    Status = "error",
                    ErrorMessage = $"Failed to get payment status: {ex.StripeError?.Message ?? ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StripeProvider: Error getting payment status for {TransactionId}", transactionId);
                return new PaymentStatusResult
                {
                    TransactionId = transactionId,
                    Status = "error",
                    ErrorMessage = "Failed to get payment status"
                };
            }
        }

        public async Task<bool> VerifyPaymentAsync(string transactionId, decimal amount)
        {
            try
            {
                _logger.LogInformation("StripeProvider: Verifying payment {TransactionId} for amount {Amount}",
                    transactionId, amount);

                var secretKey = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.StripeSecretKey);
                if (string.IsNullOrEmpty(secretKey))
                {
                    _logger.LogError("StripeProvider: Stripe is not configured");
                    return false;
                }

                StripeConfiguration.ApiKey = secretKey;

                var statusResult = await GetPaymentStatusAsync(transactionId);

                if (statusResult.Status != "completed")
                {
                    _logger.LogWarning("StripeProvider: Payment {TransactionId} is not completed. Status: {Status}",
                        transactionId, statusResult.Status);
                    return false;
                }

                // Verify amount matches
                if (statusResult.Amount.HasValue)
                {
                    var expectedAmountInCents = (long)(amount * 100);
                    var actualAmountInCents = (long)(statusResult.Amount.Value * 100);

                    if (expectedAmountInCents != actualAmountInCents)
                    {
                        _logger.LogError("StripeProvider: Amount mismatch for {TransactionId}. Expected: {Expected}, Actual: {Actual}",
                            transactionId, amount, statusResult.Amount.Value);
                        return false;
                    }
                }

                _logger.LogInformation("StripeProvider: Payment {TransactionId} verified successfully", transactionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StripeProvider: Error verifying payment {TransactionId}", transactionId);
                return false;
            }
        }

        public async Task<List<Domain.Payments.PaymentMethod>> GetPaymentMethodsAsync(string currency = "PLN")
        {
            try
            {
                _logger.LogInformation("StripeProvider: Getting payment methods for currency {Currency}", currency);

                var methods = new List<Domain.Payments.PaymentMethod>
                {
                    new Domain.Payments.PaymentMethod
                    {
                        Id = "card",
                        Name = "card",
                        DisplayName = "Credit/Debit Card",
                        Description = "Pay with Visa, Mastercard, American Express, or other cards",
                        IconUrl = "https://stripe.com/img/payment-methods/card.svg",
                        IsActive = true,
                        IsAvailable = true,
                        SupportedCurrencies = SupportedCurrencies,
                        ProcessingTime = "Instant",
                        Type = PaymentMethodType.CreditCard,
                        ProviderData = new Dictionary<string, object>
                        {
                            { "stripe_payment_method_type", "card" }
                        }
                    },
                    new Domain.Payments.PaymentMethod
                    {
                        Id = "klarna",
                        Name = "klarna",
                        DisplayName = "Klarna",
                        Description = "Buy now, pay later with Klarna",
                        IconUrl = "https://stripe.com/img/payment-methods/klarna.svg",
                        IsActive = true,
                        IsAvailable = currency == "EUR" || currency == "SEK" || currency == "NOK" || currency == "DKK",
                        SupportedCurrencies = new List<string> { "EUR", "SEK", "NOK", "DKK" },
                        ProcessingTime = "Instant",
                        Type = PaymentMethodType.DigitalWallet,
                        ProviderData = new Dictionary<string, object>
                        {
                            { "stripe_payment_method_type", "klarna" }
                        }
                    },
                    new Domain.Payments.PaymentMethod
                    {
                        Id = "apple_pay",
                        Name = "apple_pay",
                        DisplayName = "Apple Pay",
                        Description = "Pay with Touch ID, Face ID, or passcode",
                        IconUrl = "https://stripe.com/img/payment-methods/apple-pay.svg",
                        IsActive = true,
                        IsAvailable = true,
                        SupportedCurrencies = SupportedCurrencies,
                        ProcessingTime = "Instant",
                        Type = PaymentMethodType.DigitalWallet,
                        ProviderData = new Dictionary<string, object>
                        {
                            { "stripe_payment_method_type", "apple_pay" }
                        }
                    },
                    new Domain.Payments.PaymentMethod
                    {
                        Id = "google_pay",
                        Name = "google_pay",
                        DisplayName = "Google Pay",
                        Description = "Pay with your Google account",
                        IconUrl = "https://stripe.com/img/payment-methods/google-pay.svg",
                        IsActive = true,
                        IsAvailable = true,
                        SupportedCurrencies = SupportedCurrencies,
                        ProcessingTime = "Instant",
                        Type = PaymentMethodType.DigitalWallet,
                        ProviderData = new Dictionary<string, object>
                        {
                            { "stripe_payment_method_type", "google_pay" }
                        }
                    }
                };

                // Filter by currency availability
                return methods.FindAll(m => m.IsAvailable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StripeProvider: Error getting payment methods for currency {Currency}", currency);
                return new List<Domain.Payments.PaymentMethod>();
            }
        }

        public string GeneratePaymentUrl(string transactionId)
        {
            return $"https://checkout.stripe.com/pay/{transactionId}";
        }

        private async Task<bool> IsEnabledAsync()
        {
            return await _settingProvider.GetAsync<bool>(MPSettings.PaymentProviders.StripeEnabled);
        }
    }
}