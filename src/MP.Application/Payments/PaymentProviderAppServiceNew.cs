using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Volo.Abp.Application.Services;
using Volo.Abp.Users;
using Volo.Abp.Domain.Repositories;
using MP.Application.Contracts.Payments;
using MP.Domain.Payments;
using MP.Domain.Rentals;
using MP.Domain.Booths;
using Microsoft.Extensions.Logging;
using Volo.Abp.MultiTenancy;
using Newtonsoft.Json;
using Volo.Abp.SettingManagement;
using MP.Domain.Settings;

namespace MP.Application.Payments
{
    /// <summary>
    /// New multi-provider implementation of PaymentProviderAppService
    /// </summary>
    public class PaymentProviderAppServiceNew : ApplicationService, IPaymentProviderAppService
    {
        private readonly IPaymentProviderFactory _providerFactory;
        private readonly IRepository<Rental, Guid> _rentalRepository;
        private readonly IBoothRepository _boothRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentProviderAppServiceNew> _logger;
        private readonly ICurrentTenant _currentTenant;
        private readonly IP24TransactionRepository _p24TransactionRepository;
        private readonly IStripeTransactionRepository _stripeTransactionRepository;
        private readonly IPayPalTransactionRepository _payPalTransactionRepository;
        private readonly ISettingManager _settingManager;

        public PaymentProviderAppServiceNew(
            IPaymentProviderFactory providerFactory,
            IRepository<Rental, Guid> rentalRepository,
            IBoothRepository boothRepository,
            ICurrentUser currentUser,
            IConfiguration configuration,
            ILogger<PaymentProviderAppServiceNew> logger,
            ICurrentTenant currentTenant,
            IP24TransactionRepository p24TransactionRepository,
            IStripeTransactionRepository stripeTransactionRepository,
            IPayPalTransactionRepository payPalTransactionRepository,
            ISettingManager settingManager)
        {
            _providerFactory = providerFactory;
            _rentalRepository = rentalRepository;
            _boothRepository = boothRepository;
            _currentUser = currentUser;
            _configuration = configuration;
            _logger = logger;
            _currentTenant = currentTenant;
            _p24TransactionRepository = p24TransactionRepository;
            _stripeTransactionRepository = stripeTransactionRepository;
            _payPalTransactionRepository = payPalTransactionRepository;
            _settingManager = settingManager;
        }

        public async Task<List<PaymentProviderDto>> GetAvailableProvidersAsync()
        {
            try
            {
                _logger.LogInformation("PaymentProviderAppService: Getting available payment providers");

                var providers = await _providerFactory.GetAvailableProvidersAsync();

                var providerDtos = providers.Select(p => new PaymentProviderDto
                {
                    Id = p.ProviderId,
                    Name = p.ProviderId,
                    DisplayName = p.DisplayName,
                    Description = p.Description,
                    LogoUrl = p.LogoUrl,
                    SupportedCurrencies = p.SupportedCurrencies,
                    //IsActive = p.IsActive,
                    IsActive = true
                }).ToList();

                _logger.LogInformation("PaymentProviderAppService: Returning {Count} available providers: {Providers}",
                    providerDtos.Count, string.Join(", ", providerDtos.Select(p => p.Id)));

                return providerDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PaymentProviderAppService: Error getting available providers");
                return new List<PaymentProviderDto>();
            }
        }

        public async Task<List<PaymentMethodDto>> GetPaymentMethodsAsync(string providerId, string currency)
        {
            try
            {
                _logger.LogInformation("PaymentProviderAppService: GetPaymentMethodsAsync called for provider {ProviderId}, currency {Currency}",
                    providerId, currency);

                var provider = await _providerFactory.GetProviderAsync(providerId);
                if (provider == null)
                {
                    _logger.LogWarning("PaymentProviderAppService: Provider {ProviderId} not found or disabled", providerId);
                    return new List<PaymentMethodDto>();
                }

                var methods = await provider.GetPaymentMethodsAsync(currency);

                var methodDtos = methods.Select(m => new PaymentMethodDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    DisplayName = m.DisplayName,
                    Description = m.Description,
                    IconUrl = m.IconUrl,
                    ProcessingTime = m.ProcessingTime,
                    IsActive = m.IsActive && m.IsAvailable,
                    Fees = CreateFeesDto(m)
                }).ToList();

                _logger.LogInformation("PaymentProviderAppService: Retrieved {Count} payment methods from {ProviderId} for currency {Currency}",
                    methodDtos.Count, providerId, currency);

                return methodDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PaymentProviderAppService: Error getting payment methods for provider {ProviderId}, currency {Currency}",
                    providerId, currency);
                return new List<PaymentMethodDto>();
            }
        }

        public async Task<PaymentCreationResultDto> CreatePaymentAsync(CreatePaymentRequestDto request)
        {
            try
            {
                _logger.LogInformation("PaymentProviderAppService: CreatePaymentAsync called with provider {ProviderId}, amount {Amount} {Currency}",
                    request.ProviderId, request.Amount, request.Currency);

                var provider = await _providerFactory.GetProviderAsync(request.ProviderId);
                if (provider == null)
                {
                    return new PaymentCreationResultDto
                    {
                        Success = false,
                        ErrorMessage = $"Payment provider '{request.ProviderId}' is not available"
                    };
                }

                // Extract rental IDs from metadata (support both single rentalId and multiple rentalIds)
                List<Guid> rentalIds = new();

                if (request.Metadata.TryGetValue("rentalIds", out var rentalIdsObj))
                {
                    // Handle multiple rentals (cart checkout)
                    if (rentalIdsObj is IEnumerable<Guid> guidList)
                    {
                        // Direct List<Guid> from CartAppService
                        rentalIds.AddRange(guidList);
                    }
                    else if (rentalIdsObj is string rentalIdsString && !string.IsNullOrEmpty(rentalIdsString))
                    {
                        // Handle comma-separated string of GUIDs (from CartAppService)
                        var guidStrings = rentalIdsString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var guidString in guidStrings)
                        {
                            if (Guid.TryParse(guidString.Trim(), out var rentalId))
                            {
                                rentalIds.Add(rentalId);
                            }
                        }
                    }
                    else if (rentalIdsObj is IEnumerable enumerable && rentalIdsObj is not string)
                    {
                        // Fallback for other enumerable types (but not string which is IEnumerable<char>)
                        foreach (var idObj in enumerable)
                        {
                            if (Guid.TryParse(idObj?.ToString(), out var rentalId))
                            {
                                rentalIds.Add(rentalId);
                            }
                        }
                    }
                }
                else if (request.Metadata.TryGetValue("rentalId", out var rentalIdObj) &&
                         Guid.TryParse(rentalIdObj?.ToString(), out var singleRentalId))
                {
                    // Handle single rental (backward compatibility)
                    rentalIds.Add(singleRentalId);
                }

                if (rentalIds.Count == 0)
                {
                    return new PaymentCreationResultDto
                    {
                        Success = false,
                        ErrorMessage = "Invalid rental ID(s) in metadata"
                    };
                }

                // Get all rentals and verify user owns them
                var rentals = new List<Rental>();
                foreach (var rentalId in rentalIds)
                {
                    var rental = await _rentalRepository.GetAsync(rentalId);
                    if (rental.UserId != _currentUser.GetId())
                    {
                        return new PaymentCreationResultDto
                        {
                            Success = false,
                            ErrorMessage = "Access denied"
                        };
                    }
                    rentals.Add(rental);
                }

                // Create generic payment request
                // IMPORTANT: SessionId max length is 100 characters for P24
                string sessionId;
                if (rentalIds.Count == 1)
                {
                    // Single rental - use short format (first 8 chars of GUID)
                    var rentalShort = rentalIds[0].ToString("N").Substring(0, 8);
                    sessionId = $"rental_{rentalShort}_{DateTime.Now:yyyyMMddHHmmss}";
                }
                else
                {
                    // Cart checkout - use cart ID or generate short ID
                    var cartId = request.Metadata.TryGetValue("cartId", out var cartIdObj) ? cartIdObj?.ToString() : null;
                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

                    if (cartId != null && Guid.TryParse(cartId, out var cartGuid))
                    {
                        var cartShort = cartGuid.ToString("N").Substring(0, 8);
                        sessionId = $"cart_{cartShort}_{rentalIds.Count}items_{timestamp}";
                    }
                    else
                    {
                        // Fallback: use first rental's short ID
                        var rentalShort = rentalIds[0].ToString("N").Substring(0, 8);
                        sessionId = $"cart_{rentalShort}_{rentalIds.Count}items_{timestamp}";
                    }
                }

                // Ensure SessionId doesn't exceed 100 characters
                if (sessionId.Length > 100)
                {
                    _logger.LogWarning("SessionId too long ({Length} chars), truncating: {SessionId}",
                        sessionId.Length, sessionId);
                    sessionId = sessionId.Substring(0, 100);
                }

                // For Stripe, don't include sessionId in path - Stripe adds ?session_id={CHECKOUT_SESSION_ID} automatically
                // For other providers (P24, PayPal), include sessionId in path for backward compatibility
                var clientUrlTemplate = _configuration["App:ClientUrl"];
                var tenantName = (_currentTenant.Name ?? "default").ToLowerInvariant(); // Convert to lowercase for subdomain

                _logger.LogInformation("PaymentProviderAppService: URL Generation Debug");
                _logger.LogInformation("  ClientUrl Template: {ClientUrlTemplate}", clientUrlTemplate);
                _logger.LogInformation("  Tenant Name: {TenantName}", tenantName);

                var baseReturnUrl = string.Format(clientUrlTemplate!, tenantName) + "/rentals/payment-success";

                _logger.LogInformation("  Base Return URL: {BaseReturnUrl}", baseReturnUrl);

                var returnUrl = request.ProviderId.ToLowerInvariant() == "stripe"
                    ? baseReturnUrl
                    : baseReturnUrl + $"/{sessionId}";

                _logger.LogInformation("  Provider: {ProviderId}", request.ProviderId);
                _logger.LogInformation("  Final Return URL: {ReturnUrl}", returnUrl);

                var paymentRequest = new PaymentRequest
                {
                    MerchantId = await GetMerchantIdAsync(request.ProviderId),
                    SessionId = sessionId,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Description = request.Description,
                    Email = _currentUser.Email ?? "",
                    ClientName = _currentUser.Name ?? "Customer",
                    Country = "PL",
                    Language = "pl",
                    UrlReturn = returnUrl,
                    UrlStatus = _configuration["App:ApiUrl"] + "/api/app/rentals/payment/notification",
                    MethodId = request.MethodId,
                    Metadata = request.Metadata
                };

                var result = await provider.CreatePaymentAsync(paymentRequest);

                if (result.Success)
                {
                    _logger.LogInformation("PaymentProviderAppService: Payment provider returned success, token: {Token}, URL: {PaymentUrl}",
                        result.TransactionId, result.PaymentUrl);

                    // Store transaction record and update rentals (provider-agnostic)
                    // NOTE: StoreTransactionRecordAsync also updates all rentals with SessionId
                    try
                    {
                        _logger.LogInformation("PaymentProviderAppService: Starting transaction record storage for {RentalCount} rental(s) with SessionId {SessionId}",
                            rentals.Count, sessionId);

                        _logger.LogDebug("PaymentProviderAppService: SessionId={SessionId}, TransactionId={TransactionId}, Amount={Amount}, Currency={Currency}",
                            sessionId, result.TransactionId, request.Amount, request.Currency);

                        await StoreTransactionRecordAsync(result, rentals, request, sessionId);

                        _logger.LogInformation("PaymentProviderAppService: Transaction record successfully stored and {RentalCount} rentals updated with SessionId {SessionId}",
                            rentals.Count, sessionId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "PaymentProviderAppService: CRITICAL ERROR - Failed to store transaction record for {RentalCount} rental(s) with SessionId {SessionId}. Payment provider already charged customer!",
                            rentals.Count, sessionId);
                        throw;
                    }

                    _logger.LogInformation("PaymentProviderAppService: Payment created successfully for {RentalCount} rental(s), SessionId {SessionId}",
                        rentals.Count, sessionId);

                    return new PaymentCreationResultDto
                    {
                        Success = true,
                        PaymentUrl = result.PaymentUrl,
                        TransactionId = result.TransactionId
                    };
                }
                else
                {
                    _logger.LogError("PaymentProviderAppService: Payment creation failed for {RentalCount} rental(s) {RentalIds}: {Error}",
                        rentals.Count, string.Join(", ", rentalIds), result.ErrorMessage);

                    return new PaymentCreationResultDto
                    {
                        Success = false,
                        ErrorMessage = result.ErrorMessage
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PaymentProviderAppService: Error creating payment for request {@Request}", request);
                return new PaymentCreationResultDto
                {
                    Success = false,
                    ErrorMessage = "Internal error occurred"
                };
            }
        }

        private PaymentMethodFeesDto? CreateFeesDto(PaymentMethod method)
        {
            // Provider-specific fee calculation can be implemented here
            return null;
        }

        private async Task<string> GetMerchantIdAsync(string providerId)
        {
            return providerId.ToLowerInvariant() switch
            {
                "przelewy24" => await _settingManager.GetOrNullForCurrentTenantAsync(MPSettings.PaymentProviders.Przelewy24MerchantId),
                "stripe" => await _settingManager.GetOrNullForCurrentTenantAsync(MPSettings.PaymentProviders.StripePublishableKey),
                "paypal" => await _settingManager.GetOrNullForCurrentTenantAsync(MPSettings.PaymentProviders.PayPalClientId),
                _ => throw new ArgumentException($"Unknown provider: {providerId}")
            };
        }

        private async Task StoreTransactionRecordAsync(PaymentResult result, List<Rental> rentals, CreatePaymentRequestDto request, string sessionId)
        {
            // Store provider-agnostic transaction record
            // For backwards compatibility with P24Transaction, we'll adapt the data
            if (request.ProviderId.Equals("Przelewy24", StringComparison.OrdinalIgnoreCase))
            {
                // Create ONE transaction record for the entire cart payment
                var merchantId = await _settingManager.GetOrNullForCurrentTenantAsync(MPSettings.PaymentProviders.Przelewy24MerchantId);
                var posId = await _settingManager.GetOrNullForCurrentTenantAsync(MPSettings.PaymentProviders.Przelewy24PosId);

                // Use the SAME SessionId that was sent to P24
                // This is critical for matching P24 callbacks with our database records
                var p24Transaction = new P24Transaction(
                    GuidGenerator.Create(),
                    sessionId, // Use same SessionId sent to P24
                    int.Parse(merchantId ?? "142798"), // fallback value
                    int.Parse(posId ?? "142798"), // fallback value
                    request.Amount, // Total amount for entire cart
                    request.Currency,
                    _currentUser.Email ?? "",
                    request.Description, // Description for entire cart
                    "",
                    _currentTenant.Id
                );

                // Build OrderId with rental references
                string orderId;
                if (rentals.Count == 1)
                {
                    p24Transaction.SetRentalId(rentals[0].Id);
                    orderId = $"RENTAL_{rentals[0].Id}";
                }
                else
                {
                    // For multiple rentals, leave RentalId as null - use SessionId to link
                    // Store full list in ExtraProperties
                    orderId = $"CART_{string.Join(",", rentals.Select(r => r.Id))}";
                }

                p24Transaction.OrderId = orderId;
                p24Transaction.Method = request.MethodId;
                p24Transaction.ExtraProperties = System.Text.Json.JsonSerializer.Serialize(new
                {
                    RentalIds = rentals.Select(r => r.Id).ToList(),
                    CartId = request.Metadata.TryGetValue("cartId", out var cartId) ? cartId : null,
                    ItemCount = rentals.Count
                });
                p24Transaction.SetStatus("processing");

                _logger.LogDebug("PaymentProviderAppService: Inserting P24Transaction - Id={TransactionId}, SessionId={SessionId}, Amount={Amount}, Status=processing",
                    p24Transaction.Id, p24Transaction.SessionId, p24Transaction.Amount);

                await _p24TransactionRepository.InsertAsync(p24Transaction);

                _logger.LogInformation("PaymentProviderAppService: P24Transaction successfully inserted into database - Id={TransactionId}, SessionId={SessionId}",
                    p24Transaction.Id, p24Transaction.SessionId);

                // Update ALL rentals with the same SessionId
                // This creates the link: Rental.Payment.Przelewy24TransactionId == P24Transaction.SessionId
                foreach (var rental in rentals)
                {
                    _logger.LogDebug("PaymentProviderAppService: Updating rental {RentalId} with SessionId {SessionId}", rental.Id, sessionId);
                    rental.Payment.SetTransactionId(sessionId);
                    await _rentalRepository.UpdateAsync(rental);
                    _logger.LogDebug("PaymentProviderAppService: Rental {RentalId} updated successfully with SessionId {SessionId}", rental.Id, sessionId);
                }

                _logger.LogInformation("PaymentProviderAppService: Stored P24 transaction with SessionId {SessionId} for {Count} rental(s)",
                    sessionId, rentals.Count);
            }
            else if (request.ProviderId.Equals("Stripe", StringComparison.OrdinalIgnoreCase))
            {
                // Create ONE Stripe transaction record for the entire cart payment
                var stripeSessionId = result.TransactionId; // This is the Stripe Checkout Session ID
                var stripePaymentIntentId = result.ProviderData.TryGetValue("stripe_payment_intent_id", out var piId)
                    ? piId.ToString() : null;

                // Convert amount to cents for Stripe
                var amountCents = (long)(request.Amount * 100);

                var stripeTransaction = new StripeTransaction(
                    GuidGenerator.Create(),
                    stripeSessionId, // Use Checkout Session ID as PaymentIntent ID initially
                    amountCents,
                    request.Amount,
                    request.Currency.ToLowerInvariant(),
                    request.Description,
                    _currentUser.Email ?? "",
                    _currentTenant.Id
                );

                // Store metadata
                stripeTransaction.ClientSecret = stripePaymentIntentId;
                stripeTransaction.SetPaymentMethod(request.MethodId ?? "card", request.MethodId ?? "card");
                stripeTransaction.ReturnUrl = string.Format(_configuration["App:ClientUrl"]!, _currentTenant.Name ?? "default") + "/rentals/payment-success";

                // Link to rentals
                if (rentals.Count == 1)
                {
                    stripeTransaction.SetRentalId(rentals[0].Id);
                }

                // Store rental IDs in metadata
                stripeTransaction.StripeMetadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    RentalIds = rentals.Select(r => r.Id).ToList(),
                    CartId = request.Metadata.TryGetValue("cartId", out var cartId) ? cartId : null,
                    ItemCount = rentals.Count,
                    SessionId = stripeSessionId
                });

                await _stripeTransactionRepository.InsertAsync(stripeTransaction);

                // Update ALL rentals with the SessionId (using Przelewy24TransactionId field for compatibility)
                foreach (var rental in rentals)
                {
                    rental.Payment.SetTransactionId(stripeSessionId);
                    await _rentalRepository.UpdateAsync(rental);
                }

                _logger.LogInformation("Stored Stripe transaction with SessionId {SessionId} for {Count} rental(s)",
                    stripeSessionId, rentals.Count);
            }
            else if (request.ProviderId.Equals("PayPal", StringComparison.OrdinalIgnoreCase))
            {
                // Create ONE PayPal transaction record for the entire cart payment
                var paypalOrderId = result.TransactionId; // This is the PayPal Order ID

                var paypalTransaction = new PayPalTransaction(
                    GuidGenerator.Create(),
                    paypalOrderId,
                    request.Amount,
                    request.Currency,
                    _currentUser.Email ?? "",
                    request.Description,
                    "",
                    _currentTenant.Id
                );

                paypalTransaction.SetStatus("processing");

                // Link to rentals
                if (rentals.Count == 1)
                {
                    paypalTransaction.SetRentalId(rentals[0].Id);
                }

                // Store rental IDs in notes field (PayPalTransaction doesn't have ExtraProperties)
                var metadataJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    RentalIds = rentals.Select(r => r.Id).ToList(),
                    CartId = request.Metadata.TryGetValue("cartId", out var cartId) ? cartId : null,
                    ItemCount = rentals.Count
                });
                // Note: PayPalTransaction doesn't have a metadata field, rental IDs are stored via RentalId field

                await _payPalTransactionRepository.InsertAsync(paypalTransaction);

                // Update ALL rentals with the PayPal Order ID
                foreach (var rental in rentals)
                {
                    rental.Payment.SetTransactionId(paypalOrderId);
                    await _rentalRepository.UpdateAsync(rental);
                }

                _logger.LogInformation("Stored PayPal transaction with OrderId {OrderId} for {Count} rental(s)",
                    paypalOrderId, rentals.Count);
            }
            else
            {
                _logger.LogWarning("PaymentProviderAppService: Transaction record storage for provider {ProviderId} not implemented",
                    request.ProviderId);
            }
        }
    }
}