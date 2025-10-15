using System;
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
using Volo.Abp.Settings;
using MP.Domain.Settings;

namespace MP.Application.Payments
{
    public class PaymentProviderAppService : ApplicationService, IPaymentProviderAppService
    {
        private readonly IPrzelewy24Service _przelewy24Service;
        private readonly IRepository<Rental, Guid> _rentalRepository;
        private readonly IBoothRepository _boothRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentProviderAppService> _logger;
        private readonly ICurrentTenant _currentTenant;
        private readonly IP24TransactionRepository _p24TransactionRepository;
        private readonly ISettingProvider _settingProvider;

        public PaymentProviderAppService(
            IPrzelewy24Service przelewy24Service,
            IRepository<Rental, Guid> rentalRepository,
            IBoothRepository boothRepository,
            ICurrentUser currentUser,
            IConfiguration configuration,
            ILogger<PaymentProviderAppService> logger,
            ICurrentTenant currentTenant,
            IP24TransactionRepository p24TransactionRepository,
            ISettingProvider settingProvider)
        {
            _przelewy24Service = przelewy24Service;
            _rentalRepository = rentalRepository;
            _boothRepository = boothRepository;
            _currentUser = currentUser;
            _configuration = configuration;
            _logger = logger;
            _currentTenant = currentTenant;
            _p24TransactionRepository = p24TransactionRepository;
            _settingProvider = settingProvider;
        }

        public async Task<List<PaymentProviderDto>> GetAvailableProvidersAsync()
        {
            var providers = new List<PaymentProviderDto>();

            // Check if Przelewy24 is enabled for current tenant
            var isPrzelewy24Enabled = await _settingProvider.GetAsync<bool>(MPSettings.PaymentProviders.Przelewy24Enabled);

            if (isPrzelewy24Enabled)
            {
                // Verify that required settings are configured
                var merchantId = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.Przelewy24MerchantId);
                var posId = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.Przelewy24PosId);
                var apiKey = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.Przelewy24ApiKey);
                var crcKey = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.Przelewy24CrcKey);

                var isConfigured = !string.IsNullOrEmpty(merchantId) &&
                                 !string.IsNullOrEmpty(posId) &&
                                 !string.IsNullOrEmpty(apiKey) &&
                                 !string.IsNullOrEmpty(crcKey);

                if (isConfigured)
                {
                    // Check if using sandbox based on configuration URL
                    var baseUrl = _configuration["PaymentProviders:Przelewy24:BaseUrl"] ?? "";
                    var isSandbox = baseUrl.Contains("sandbox");

                    providers.Add(new PaymentProviderDto
                    {
                        Id = "przelewy24",
                        Name = "Przelewy24",
                        DisplayName = "Przelewy24",
                        Description = isSandbox ? "Secure online payments with Przelewy24 (Sandbox)" : "Secure online payments with Przelewy24",
                        LogoUrl = "https://www.przelewy24.pl/themes/przelewy24/assets/img/base/przelewy24_logo_2022.svg",
                        SupportedCurrencies = new List<string> { "PLN", "EUR", "USD", "GBP" },
                        IsActive = true
                    });

                    _logger.LogInformation("Przelewy24 payment provider enabled for tenant {TenantId} (Sandbox: {IsSandbox})",
                        _currentTenant.Id, isSandbox);
                }
                else
                {
                    _logger.LogWarning("Przelewy24 is enabled but not properly configured for tenant {TenantId}. Missing: MerchantId={HasMerchantId}, PosId={HasPosId}, ApiKey={HasApiKey}, CrcKey={HasCrcKey}",
                        _currentTenant.Id, !string.IsNullOrEmpty(merchantId), !string.IsNullOrEmpty(posId), !string.IsNullOrEmpty(apiKey), !string.IsNullOrEmpty(crcKey));
                }
            }
            else
            {
                _logger.LogInformation("Przelewy24 payment provider is disabled for tenant {TenantId}", _currentTenant.Id);
            }

            return providers;
        }

        public async Task<List<PaymentMethodDto>> GetPaymentMethodsAsync(string providerId, string currency)
        {
            _logger.LogInformation("PaymentProviderAppService: GetPaymentMethodsAsync called for provider {ProviderId}, currency {Currency}", providerId, currency);

            if (providerId != "przelewy24")
            {
                _logger.LogWarning("PaymentProviderAppService: Unsupported provider {ProviderId}", providerId);
                return new List<PaymentMethodDto>();
            }

            // Pobierz metody płatności z API Przelewy24
            var p24Methods = await _przelewy24Service.GetPaymentMethodsAsync(currency);

            // Konwertuj na DTOs
            var methods = p24Methods.Select(m => new PaymentMethodDto
            {
                Id = m.Id.ToString(),
                Name = m.Name,
                DisplayName = m.DisplayName,
                Description = m.Description,
                IconUrl = m.IconUrl,
                ProcessingTime = m.ProcessingTime,
                IsActive = m.IsActive && m.IsAvailable,
                Fees = CreateFeesDto(m)
            }).ToList();

            _logger.LogInformation("Retrieved {Count} payment methods from Przelewy24 for currency {Currency}",
                methods.Count, currency);

            return methods;
        }

        private PaymentMethodFeesDto? CreateFeesDto(Przelewy24PaymentMethod method)
        {
            // Można dodać informacje o opłatach jeśli są dostępne w API P24
            // Na razie zwracamy null - opłaty są zazwyczaj po stronie merchant'a
            return null;
        }

        public async Task<PaymentCreationResultDto> CreatePaymentAsync(CreatePaymentRequestDto request)
        {
            _logger.LogInformation("PaymentProviderAppService: CreatePaymentAsync called with request: {@Request}", request);

            try
            {
                if (request.ProviderId != "przelewy24")
                {
                    return new PaymentCreationResultDto
                    {
                        Success = false,
                        ErrorMessage = "Unsupported payment provider"
                    };
                }

                // Extract rental IDs from metadata - support both single rental and cart checkout
                List<Guid> rentalIds = new List<Guid>();

                // Try to get rentalIds array (from cart checkout)
                if (request.Metadata.TryGetValue("rentalIds", out var rentalIdsObj))
                {
                    if (rentalIdsObj is System.Text.Json.JsonElement jsonElement && jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var item in jsonElement.EnumerateArray())
                        {
                            if (Guid.TryParse(item.GetString(), out var guid))
                            {
                                rentalIds.Add(guid);
                            }
                        }
                    }
                    else if (rentalIdsObj is List<object> list)
                    {
                        foreach (var item in list)
                        {
                            if (Guid.TryParse(item?.ToString(), out var guid))
                            {
                                rentalIds.Add(guid);
                            }
                        }
                    }
                }
                // Fallback to single rentalId (for backwards compatibility)
                else if (request.Metadata.TryGetValue("rentalId", out var rentalIdObj) &&
                         Guid.TryParse(rentalIdObj?.ToString(), out var rentalId))
                {
                    rentalIds.Add(rentalId);
                }

                if (rentalIds.Count == 0)
                {
                    return new PaymentCreationResultDto
                    {
                        Success = false,
                        ErrorMessage = "Invalid rental ID(s) in metadata"
                    };
                }

                // Load all rentals and verify ownership
                var rentals = new List<Rental>();
                foreach (var rid in rentalIds)
                {
                    var rental = await _rentalRepository.GetAsync(rid);

                    // Verify user owns the rental
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

                // Create P24 payment request using tenant settings
                var merchantId = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.Przelewy24MerchantId);
                var posId = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.Przelewy24PosId);

                // Generate SessionId based on rental type
                // IMPORTANT: SessionId max length is 100 characters in P24
                string localSessionId;
                string orderId;

                if (rentalIds.Count > 1)
                {
                    // Cart checkout - multiple rentals
                    // Use short format to stay under 100 char limit
                    var cartId = request.Metadata.TryGetValue("cartId", out var cartIdObj) ? cartIdObj?.ToString() : null;
                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

                    if (cartId != null && Guid.TryParse(cartId, out var cartGuid))
                    {
                        // Use first 8 chars of cart ID (short form)
                        var cartShort = cartGuid.ToString("N").Substring(0, 8);
                        localSessionId = $"cart_{cartShort}_{rentalIds.Count}items_{timestamp}";
                    }
                    else
                    {
                        // Fallback: use first rental's short ID
                        var rentalShort = rentalIds[0].ToString("N").Substring(0, 8);
                        localSessionId = $"cart_{rentalShort}_{rentalIds.Count}items_{timestamp}";
                    }

                    orderId = $"CART_{string.Join(",", rentalIds.Select(id => id.ToString().ToLower()))}";
                }
                else
                {
                    // Single rental - use short format (first 8 chars of GUID)
                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    var rentalShort = rentalIds[0].ToString("N").Substring(0, 8);
                    localSessionId = $"rental_{rentalShort}_{timestamp}";
                    orderId = $"RENTAL_{rentalIds[0]}";
                }

                // Ensure SessionId doesn't exceed 100 characters
                if (localSessionId.Length > 100)
                {
                    _logger.LogWarning("SessionId too long ({Length} chars), truncating: {SessionId}",
                        localSessionId.Length, localSessionId);
                    localSessionId = localSessionId.Substring(0, 100);
                }

                var p24Request = new Przelewy24PaymentRequest
                {
                    MerchantId = merchantId,
                    PosId = posId,
                    SessionId = localSessionId,
                    Amount = request.Amount, // Amount already in correct format for P24
                    Currency = request.Currency,
                    Description = request.Description,
                    Email = _currentUser.Email ?? "",
                    ClientName = _currentUser.Name ?? "Klient",
                    Country = "PL",
                    Language = "pl",
                    UrlReturn = string.Format(_configuration["App:ClientUrl"]!, _currentTenant.Name ?? "default") + $"/rentals/payment-success/{localSessionId}",
                    UrlStatus = _configuration["App:ApiUrl"] + "/api/app/rentals/payment/notification"
                };

                // Add method ID if specified
                if (!string.IsNullOrEmpty(request.MethodId))
                {
                    // In real P24 integration, you would pass method ID to force specific payment method
                    _logger.LogInformation("Payment method {MethodId} selected for {Count} rental(s)",
                        request.MethodId, rentalIds.Count);
                }

                var result = await _przelewy24Service.CreatePaymentAsync(p24Request);

                // Create P24Transaction record regardless of result
                var extraProperties = new
                {
                    UrlStatus = p24Request.UrlStatus,
                    UrlReturn = p24Request.UrlReturn,
                    PaymentProviderId = request.ProviderId,
                    PaymentMethodId = request.MethodId,
                    RentalIds = rentalIds // Store all rental IDs
                };

                // Store the original local session ID (this is what P24 will use and return)
                var p24SessionId = localSessionId;

                if (result.Success)
                {
                    // P24 returns a token, but we use our SessionId for tracking
                    var p24Transaction = new P24Transaction(
                        GuidGenerator.Create(),
                        p24SessionId, // Use our SessionId that P24 will reference
                        int.Parse(p24Request.MerchantId),
                        int.Parse(p24Request.PosId),
                        p24Request.Amount,
                        p24Request.Currency,
                        p24Request.Email,
                        p24Request.Description,
                        "", // Sign will be calculated in service
                        _currentTenant.Id
                    );

                    // For single rental, set RentalId for backward compatibility
                    // For cart checkout (multiple rentals), leave RentalId as null - use SessionId to link
                    if (rentalIds.Count == 1)
                    {
                        p24Transaction.SetRentalId(rentalIds[0]);
                    }

                    p24Transaction.Method = request.MethodId;
                    p24Transaction.ReturnUrl = p24Request.UrlReturn;
                    p24Transaction.OrderId = orderId; // Store order ID with rental references
                    p24Transaction.ExtraProperties = JsonConvert.SerializeObject(extraProperties);
                    p24Transaction.SetStatus("processing");

                    // Update all rentals with the same SessionId
                    // This creates the link: Rental.Payment.Przelewy24TransactionId == P24Transaction.SessionId
                    foreach (var rental in rentals)
                    {
                        rental.Payment.SetTransactionId(p24SessionId);
                        await _rentalRepository.UpdateAsync(rental);
                    }

                    await _p24TransactionRepository.InsertAsync(p24Transaction);
                }
                else
                {
                    // For failed transactions, still store with our local session ID
                    var p24Transaction = new P24Transaction(
                        GuidGenerator.Create(),
                        p24SessionId,
                        int.Parse(p24Request.MerchantId),
                        int.Parse(p24Request.PosId),
                        p24Request.Amount,
                        p24Request.Currency,
                        p24Request.Email,
                        p24Request.Description,
                        "", // Sign will be calculated in service
                        _currentTenant.Id
                    );

                    // For single rental, set RentalId for backward compatibility
                    if (rentalIds.Count == 1)
                    {
                        p24Transaction.SetRentalId(rentalIds[0]);
                    }

                    p24Transaction.Method = request.MethodId;
                    p24Transaction.ReturnUrl = p24Request.UrlReturn;
                    p24Transaction.OrderId = orderId;
                    p24Transaction.ExtraProperties = JsonConvert.SerializeObject(extraProperties);
                    p24Transaction.SetStatus("failed");
                    await _p24TransactionRepository.InsertAsync(p24Transaction);
                }

                // Note: Status checking is now handled by Hangfire recurring job (runs every 15 minutes)
                // See P24StatusCheckRecurringJob for automatic payment status verification

                if (result.Success)
                {
                    _logger.LogInformation("Payment created for {Count} rental(s), SessionId {SessionId}, PaymentUrl {PaymentUrl}",
                        rentalIds.Count, p24SessionId, result.PaymentUrl);

                    return new PaymentCreationResultDto
                    {
                        Success = true,
                        PaymentUrl = result.PaymentUrl,
                        TransactionId = p24SessionId // Return SessionId instead of P24 token
                    };
                }
                else
                {
                    _logger.LogError("Failed to create payment for {Count} rental(s): {Error}",
                        rentalIds.Count, result.ErrorMessage);

                    return new PaymentCreationResultDto
                    {
                        Success = false,
                        ErrorMessage = result.ErrorMessage
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment for request {@Request}", request);
                return new PaymentCreationResultDto
                {
                    Success = false,
                    ErrorMessage = "Internal error occurred"
                };
            }
        }
    }
}