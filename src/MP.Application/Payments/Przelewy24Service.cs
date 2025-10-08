using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.Application.Services;
using Volo.Abp;
using Volo.Abp.Settings;
using MP.Domain.Payments;
using MP.Domain.Settings;

namespace MP.Application.Payments
{
    public class Przelewy24Service : ApplicationService, IPrzelewy24Service
    {
        private readonly HttpClient _httpClient;
        private readonly ISettingProvider _settingProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<Przelewy24Service> _logger;

        public Przelewy24Service(
            HttpClient httpClient,
            ISettingProvider settingProvider,
            IConfiguration configuration,
            ILogger<Przelewy24Service> logger)
        {
            _httpClient = httpClient;
            _settingProvider = settingProvider;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<Przelewy24PaymentResult> CreatePaymentAsync(Przelewy24PaymentRequest request)
        {
            try
            {
                // Get settings from ABP Settings
                var merchantId = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.Przelewy24MerchantId);
                var posId = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.Przelewy24PosId);
                var apiKey = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.Przelewy24ApiKey);
                var crcKey = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.Przelewy24CrcKey);

                // Get base URL from appsettings.json
                var baseUrl = _configuration["PaymentProviders:Przelewy24:BaseUrl"] ?? "https://sandbox.przelewy24.pl";

                // Konwertuj złotówki na grosze (Przelewy24 wymaga kwoty w groszach)
                var amountInGrosze = (int)(request.Amount * 100);
                var merchantIdInt = int.Parse(merchantId);
                var posIdInt = int.Parse(posId);

                // Generuj podpis CRC
                var sign = GenerateSignature(request.SessionId, merchantIdInt, amountInGrosze, request.Currency, crcKey);

                var payload = new
                {
                    merchantId = merchantIdInt,
                    posId = posIdInt,
                    sessionId = request.SessionId,
                    amount = amountInGrosze,
                    currency = request.Currency,
                    description = request.Description,
                    email = request.Email,
                    client = request.ClientName,
                    country = request.Country,
                    language = request.Language,
                    urlReturn = request.UrlReturn,
                    urlStatus = request.UrlStatus,
                    timeLimit = 15, // 15 minut na płatność
                    sign = sign
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Dodaj autoryzację
                var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{posId}:{apiKey}"));
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

                var response = await _httpClient.PostAsync($"{baseUrl}/api/v1/transaction/register", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Przelewy24 response: {Response}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    dynamic result = JsonConvert.DeserializeObject(responseContent)!;
                    var token = result.data.token.ToString();

                    return new Przelewy24PaymentResult
                    {
                        TransactionId = token,
                        PaymentUrl = GeneratePaymentUrl(token),
                        Success = true
                    };
                }
                else
                {
                    _logger.LogError("Przelewy24 payment creation failed: {Response}", responseContent);
                    return new Przelewy24PaymentResult
                    {
                        Success = false,
                        ErrorMessage = $"Payment creation failed: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Przelewy24 payment");
                return new Przelewy24PaymentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<Przelewy24PaymentStatus> GetPaymentStatusAsync(string transactionId)
        {
            try
            {
                var posId = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.Przelewy24PosId);
                var apiKey = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.Przelewy24ApiKey);

                // Get base URL from appsettings.json
                var baseUrl = _configuration["PaymentProviders:Przelewy24:BaseUrl"] ?? "https://sandbox.przelewy24.pl";

                var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{posId}:{apiKey}"));
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

                var response = await _httpClient.GetAsync($"{baseUrl}/api/v1/transaction/by/sessionId/{transactionId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    dynamic result = JsonConvert.DeserializeObject(responseContent)!;
                    var data = result.data;

                    return new Przelewy24PaymentStatus
                    {
                        TransactionId = transactionId,
                        Status = MapP24Status(data.status.ToString()),
                        Amount = data.amount != null ? (decimal)data.amount / 100 : null, // konwersja z groszy na złotówki
                        CompletedAt = data.completedAt != null ? DateTime.Parse(data.completedAt.ToString()) : null
                    };
                }
                else
                {
                    return new Przelewy24PaymentStatus
                    {
                        TransactionId = transactionId,
                        Status = "failed",
                        ErrorMessage = responseContent
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Przelewy24 payment status for {TransactionId}", transactionId);
                return new Przelewy24PaymentStatus
                {
                    TransactionId = transactionId,
                    Status = "failed",
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<bool> VerifyPaymentAsync(string transactionId, decimal expectedAmount)
        {
            try
            {
                var status = await GetPaymentStatusAsync(transactionId);
                return status.Status == "completed" &&
                       status.Amount.HasValue &&
                       Math.Abs(status.Amount.Value - expectedAmount) < 0.01m;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Przelewy24 payment {TransactionId}", transactionId);
                return false;
            }
        }

        public async Task<List<Przelewy24PaymentMethod>> GetPaymentMethodsAsync(string currency = "PLN")
        {
            try
            {
                var posId = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.Przelewy24PosId);
                var apiKey = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.Przelewy24ApiKey);

                // Get base URL from appsettings.json
                var baseUrl = _configuration["PaymentProviders:Przelewy24:BaseUrl"] ?? "https://sandbox.przelewy24.pl";

                // Clear previous authorization headers
                _httpClient.DefaultRequestHeaders.Authorization = null;

                // Set authorization header
                var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{posId}:{apiKey}"));
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

                // Try multiple possible endpoints for payment methods
                var language = "pl";
                var endpoints = new[] {
                    $"{baseUrl}/api/v1/payment/methods/{language}"
                };

                HttpResponseMessage response = null;
                string responseContent = "";
                string usedEndpoint = "";

                foreach (var endpoint in endpoints)
                {
                    try
                    {
                        _logger.LogInformation("Trying Przelewy24 endpoint: {Endpoint}", endpoint);
                        response = await _httpClient.GetAsync(endpoint);
                        responseContent = await response.Content.ReadAsStringAsync();
                        usedEndpoint = endpoint;

                        _logger.LogInformation("Przelewy24 response from {Endpoint}: Status={StatusCode}, Content={Response}",
                            endpoint, response.StatusCode, responseContent);

                        if (response.IsSuccessStatusCode)
                        {
                            break; // Success, use this endpoint
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to call Przelewy24 endpoint {Endpoint}", endpoint);
                    }
                }

                if (response == null)
                {
                    _logger.LogError("All Przelewy24 payment methods endpoints failed");
                    return GetFallbackPaymentMethods(currency);
                }

                if (response.IsSuccessStatusCode)
                {
                    dynamic result = JsonConvert.DeserializeObject(responseContent)!;
                    var methods = new List<Przelewy24PaymentMethod>();

                    // Parsuj odpowiedź API Przelewy24
                    if (result.data != null)
                    {
                        foreach (var method in result.data)
                        {
                            var methodId = (int)method.id;
                            var methodName = method.name?.ToString() ?? "";
                            var isActive = ParseBooleanField(method.status);
                            var isAvailable = ParseBooleanFieldNullable(method.available) ?? true;

                            // TODO: Dodać logowanie gdy rozwiążemy problem z dynamic

                            methods.Add(new Przelewy24PaymentMethod
                            {
                                Id = methodId,
                                Name = methodName,
                                DisplayName = method.displayName?.ToString() ?? methodName,
                                Description = method.description?.ToString(),
                                IconUrl = method.imgUrl?.ToString(),
                                IsActive = isActive,
                                IsAvailable = isAvailable,
                                SupportedCurrencies = ParseSupportedCurrencies(method.currencies?.ToString()),
                                ProcessingTime = GetProcessingTimeForMethod(methodId),
                                MinAmount = method.minAmount != null ? (decimal?)method.minAmount / 100 : null,
                                MaxAmount = method.maxAmount != null ? (decimal?)method.maxAmount / 100 : null
                            });
                        }
                    }

                    // Filtruj metody według waluty
                    return methods.Where(m => m.IsActive && m.IsAvailable &&
                                            (m.SupportedCurrencies.Count == 0 || m.SupportedCurrencies.Contains(currency)))
                                 .ToList();
                }
                else
                {
                    _logger.LogError("Failed to get payment methods from Przelewy24: {Response}", responseContent);
                    return GetFallbackPaymentMethods(currency);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment methods from Przelewy24");
                return GetFallbackPaymentMethods(currency);
            }
        }

        public string GeneratePaymentUrl(string transactionId)
        {
            // Get base URL from appsettings.json
            var baseUrl = _configuration["PaymentProviders:Przelewy24:BaseUrl"] ?? "https://sandbox.przelewy24.pl";
            return $"{baseUrl}/trnRequest/{transactionId}";
        }

        private string MapP24Status(string p24Status)
        {
            // Przelewy24 API status codes:
            // 1 = verified/completed
            // 2 = rejected/failed
            // 3 = cancelled
            return p24Status.ToLower() switch
            {
                "1" or "completed" => "completed",
                "2" or "rejected" or "failed" => "failed",
                "3" or "cancelled" => "cancelled",
                "0" or "pending" => "pending",
                _ => "pending"
            };
        }

        private List<string> ParseSupportedCurrencies(string? currenciesString)
        {
            if (string.IsNullOrEmpty(currenciesString))
                return new List<string> { "PLN" }; // domyślnie PLN

            try
            {
                return currenciesString.Split(',')
                    .Select(c => c.Trim().ToUpper())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList();
            }
            catch
            {
                return new List<string> { "PLN" };
            }
        }

        private string? GetProcessingTimeForMethod(int methodId)
        {
            return methodId switch
            {
                1 => "Natychmiastowa", // karty
                25 => "Natychmiastowa", // BLIK
                31 => "1-3 dni robocze", // przelew tradycyjny
                142 => "Natychmiastowa", // PayNow ING
                154 => "Natychmiastowa", // Alior Bank
                _ => null
            };
        }

        private bool ParseBooleanField(dynamic field)
        {
            if (field == null) return false;

            // Logowanie wyłączone tymczasowo - problem z dynamic
            // _logger.LogDebug("ParseBooleanField: field = {Field}, type = {Type}", fieldStr, typeStr);

            // Jeśli to już bool
            if (field is bool boolValue) return boolValue;

            // Spróbuj jako string reprezentację
            var stringValue = field.ToString();
            if (!string.IsNullOrEmpty(stringValue))
            {
                // Usuń nawiasy klamrowe jeśli są
                stringValue = stringValue.Trim('{', '}', ' ');

                if (string.Equals(stringValue, "true", StringComparison.OrdinalIgnoreCase) ||
                    stringValue == "1")
                {
                    return true;
                }

                if (string.Equals(stringValue, "false", StringComparison.OrdinalIgnoreCase) ||
                    stringValue == "0")
                {
                    return false;
                }
            }

            // Spróbuj konwersję bezpośrednią
            try
            {
                return Convert.ToBoolean(field);
            }
            catch (Exception ex)
            {
                // Logowanie wyłączone tymczasowo
                // _logger.LogWarning(ex, "Failed to parse boolean field: {Field}", field?.ToString());
                return false;
            }
        }

        private bool? ParseBooleanFieldNullable(dynamic field)
        {
            if (field == null) return null;
            return ParseBooleanField(field);
        }

        private List<Przelewy24PaymentMethod> GetFallbackPaymentMethods(string currency)
        {
            _logger.LogWarning("Using fallback payment methods for currency {Currency}", currency);

            // Rozszerzone metody zastępcze na wypadek błędu API - bazują na popularnych metodach P24
            var methods = new List<Przelewy24PaymentMethod>
            {
                new Przelewy24PaymentMethod
                {
                    Id = 1,
                    Name = "card",
                    DisplayName = "Karta płatnicza",
                    Description = "Visa, MasterCard, American Express",
                    IsActive = true,
                    IsAvailable = true,
                    SupportedCurrencies = new List<string> { "PLN", "EUR", "USD", "GBP" },
                    ProcessingTime = "Natychmiastowa"
                },
                new Przelewy24PaymentMethod
                {
                    Id = 25,
                    Name = "blik",
                    DisplayName = "BLIK",
                    Description = "Płatność mobilna BLIK",
                    IsActive = true,
                    IsAvailable = currency == "PLN",
                    SupportedCurrencies = new List<string> { "PLN" },
                    ProcessingTime = "Natychmiastowa"
                },
                new Przelewy24PaymentMethod
                {
                    Id = 142,
                    Name = "ing",
                    DisplayName = "ING Bank Śląski",
                    Description = "Płatność przez ING Bank Śląski",
                    IsActive = true,
                    IsAvailable = currency == "PLN",
                    SupportedCurrencies = new List<string> { "PLN" },
                    ProcessingTime = "Natychmiastowa"
                },
                new Przelewy24PaymentMethod
                {
                    Id = 154,
                    Name = "alior",
                    DisplayName = "Alior Bank",
                    Description = "Płatność przez Alior Bank",
                    IsActive = true,
                    IsAvailable = currency == "PLN",
                    SupportedCurrencies = new List<string> { "PLN" },
                    ProcessingTime = "Natychmiastowa"
                },
                new Przelewy24PaymentMethod
                {
                    Id = 20,
                    Name = "pekao",
                    DisplayName = "Bank Pekao",
                    Description = "Płatność przez Bank Pekao",
                    IsActive = true,
                    IsAvailable = currency == "PLN",
                    SupportedCurrencies = new List<string> { "PLN" },
                    ProcessingTime = "Natychmiastowa"
                },
                new Przelewy24PaymentMethod
                {
                    Id = 32,
                    Name = "millennium",
                    DisplayName = "Bank Millennium",
                    Description = "Płatność przez Bank Millennium",
                    IsActive = true,
                    IsAvailable = currency == "PLN",
                    SupportedCurrencies = new List<string> { "PLN" },
                    ProcessingTime = "Natychmiastowa"
                },
                new Przelewy24PaymentMethod
                {
                    Id = 65,
                    Name = "paypal",
                    DisplayName = "PayPal",
                    Description = "Płatność przez PayPal",
                    IsActive = true,
                    IsAvailable = true,
                    SupportedCurrencies = new List<string> { "PLN", "EUR", "USD", "GBP" },
                    ProcessingTime = "Natychmiastowa"
                },
                new Przelewy24PaymentMethod
                {
                    Id = 31,
                    Name = "transfer",
                    DisplayName = "Przelew tradycyjny",
                    Description = "Tradycyjny przelew bankowy",
                    IsActive = true,
                    IsAvailable = true,
                    SupportedCurrencies = new List<string> { "PLN", "EUR" },
                    ProcessingTime = "1-3 dni robocze"
                }
            };

            return methods.Where(m => m.SupportedCurrencies.Contains(currency)).ToList();
        }

        private string GenerateSignature(string sessionId, int merchantId, int amount, string currency, string crcKey)
        {
            try
            {
                // Zgodnie z dokumentacją Przelewy24, podpis jest generowany z określonych parametrów
                var signatureData = new
                {
                    sessionId = sessionId,
                    merchantId = merchantId,
                    amount = amount,
                    currency = currency,
                    crc = crcKey
                };

                // Serializuj do JSON bez escaping
                var json = JsonConvert.SerializeObject(signatureData, Formatting.None, new JsonSerializerSettings
                {
                    StringEscapeHandling = StringEscapeHandling.Default
                });

                _logger.LogDebug("Przelewy24 signature data: {Json}", json);

                // Wygeneruj hash SHA-384
                using (var sha384 = SHA384.Create())
                {
                    var bytes = Encoding.UTF8.GetBytes(json);
                    var hash = sha384.ComputeHash(bytes);
                    var signature = Convert.ToHexString(hash).ToLower();

                    _logger.LogDebug("Generated signature: {Signature}", signature);
                    return signature;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Przelewy24 signature");
                throw;
            }
        }
    }
}