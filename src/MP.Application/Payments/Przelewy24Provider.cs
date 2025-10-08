using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;
using MP.Domain.Payments;
using MP.Domain.Settings;

namespace MP.Application.Payments
{
    /// <summary>
    /// Przelewy24 implementation of IPaymentProvider
    /// </summary>
    public class Przelewy24Provider : IPaymentProvider
    {
        private readonly IPrzelewy24Service _przelewy24Service;
        private readonly ISettingProvider _settingProvider;
        private readonly ILogger<Przelewy24Provider> _logger;
        private readonly ICurrentTenant _currentTenant;

        public string ProviderId => "Przelewy24";
        public string DisplayName => "Przelewy24";
        public string Description => "Secure online payments with Przelewy24";
        public string? LogoUrl => "https://www.przelewy24.pl/themes/przelewy24/assets/img/base/przelewy24_logo_2022.svg";
        public List<string> SupportedCurrencies => new() { "PLN", "EUR", "USD", "GBP" };

        public Przelewy24Provider(
            IPrzelewy24Service przelewy24Service,
            ISettingProvider settingProvider,
            ILogger<Przelewy24Provider> logger,
            ICurrentTenant currentTenant)
        {
            _przelewy24Service = przelewy24Service;
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
                    var isEnabled = _settingProvider.GetAsync<bool>(MPSettings.PaymentProviders.Przelewy24Enabled).Result;
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
                _logger.LogInformation("Przelewy24Provider: Creating payment for amount {Amount} {Currency}",
                    request.Amount, request.Currency);

                var p24Request = new Przelewy24PaymentRequest
                {
                    MerchantId = request.MerchantId,
                    PosId = request.MerchantId, // In P24, PosId is usually same as MerchantId
                    SessionId = request.SessionId,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Description = request.Description,
                    Email = request.Email,
                    ClientName = request.ClientName,
                    Country = request.Country,
                    Language = request.Language,
                    UrlReturn = request.UrlReturn,
                    UrlStatus = request.UrlStatus
                };

                var result = await _przelewy24Service.CreatePaymentAsync(p24Request);

                return new PaymentResult
                {
                    TransactionId = result.TransactionId,
                    PaymentUrl = result.PaymentUrl,
                    Success = result.Success,
                    ErrorMessage = result.ErrorMessage,
                    ProviderData = new Dictionary<string, object>
                    {
                        { "przelewy24_merchant_id", request.MerchantId },
                        { "przelewy24_session_id", request.SessionId }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Przelewy24Provider: Error creating payment");
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
                var status = await _przelewy24Service.GetPaymentStatusAsync(transactionId);

                return new PaymentStatusResult
                {
                    TransactionId = status.TransactionId,
                    Status = status.Status,
                    Amount = status.Amount,
                    CompletedAt = status.CompletedAt,
                    ErrorCode = status.ErrorCode,
                    ErrorMessage = status.ErrorMessage,
                    ProviderData = new Dictionary<string, object>
                    {
                        { "przelewy24_status", status.Status }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Przelewy24Provider: Error getting payment status for transaction {TransactionId}", transactionId);
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
                return await _przelewy24Service.VerifyPaymentAsync(transactionId, amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Przelewy24Provider: Error verifying payment {TransactionId}", transactionId);
                return false;
            }
        }

        public async Task<List<PaymentMethod>> GetPaymentMethodsAsync(string currency = "PLN")
        {
            try
            {
                var p24Methods = await _przelewy24Service.GetPaymentMethodsAsync(currency);

                return p24Methods.Select(m => new PaymentMethod
                {
                    Id = m.Id.ToString(),
                    Name = m.Name,
                    DisplayName = m.DisplayName,
                    Description = m.Description,
                    IconUrl = m.IconUrl,
                    IsActive = m.IsActive,
                    IsAvailable = m.IsAvailable,
                    SupportedCurrencies = m.SupportedCurrencies,
                    ProcessingTime = m.ProcessingTime,
                    MinAmount = m.MinAmount,
                    MaxAmount = m.MaxAmount,
                    Type = DeterminePaymentMethodType(m.Id, m.Name),
                    ProviderData = new Dictionary<string, object>
                    {
                        { "przelewy24_method_id", m.Id },
                        { "przelewy24_name", m.Name }
                    }
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Przelewy24Provider: Error getting payment methods for currency {Currency}", currency);
                return new List<PaymentMethod>();
            }
        }

        public string GeneratePaymentUrl(string transactionId)
        {
            return _przelewy24Service.GeneratePaymentUrl(transactionId);
        }

        private async Task<bool> IsEnabledAsync()
        {
            return await _settingProvider.GetAsync<bool>(MPSettings.PaymentProviders.Przelewy24Enabled);
        }

        private static PaymentMethodType DeterminePaymentMethodType(int methodId, string name)
        {
            // Based on Przelewy24 method IDs and names
            var nameLower = name.ToLowerInvariant();

            if (nameLower.Contains("blik"))
                return PaymentMethodType.BLIK;

            if (nameLower.Contains("visa") || nameLower.Contains("mastercard") ||
                nameLower.Contains("card") || nameLower.Contains("credit"))
                return PaymentMethodType.CreditCard;

            if (nameLower.Contains("debit"))
                return PaymentMethodType.DebitCard;

            if (nameLower.Contains("paypal") || nameLower.Contains("wallet"))
                return PaymentMethodType.DigitalWallet;

            if (nameLower.Contains("transfer") || nameLower.Contains("bank"))
                return PaymentMethodType.BankTransfer;

            return PaymentMethodType.Other;
        }
    }
}