using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using MP.Domain.Payments;
using MP.Domain.Rentals;
using MP.Payments;
using MP.Rentals;

namespace MP.Application.Payments
{
    [Authorize]
    public class PaymentTransactionAppService : ApplicationService, IPaymentTransactionAppService
    {
        private readonly IRepository<P24Transaction, Guid> _p24TransactionRepository;
        private readonly IRepository<Rental, Guid> _rentalRepository;
        private readonly IP24TransactionRepository _customP24TransactionRepository;
        private readonly IStripeTransactionRepository _stripeTransactionRepository;
        private readonly IPayPalTransactionRepository _payPalTransactionRepository;

        public PaymentTransactionAppService(
            IRepository<P24Transaction, Guid> p24TransactionRepository,
            IRepository<Rental, Guid> rentalRepository,
            IP24TransactionRepository customP24TransactionRepository,
            IStripeTransactionRepository stripeTransactionRepository,
            IPayPalTransactionRepository payPalTransactionRepository)
        {
            _p24TransactionRepository = p24TransactionRepository;
            _rentalRepository = rentalRepository;
            _customP24TransactionRepository = customP24TransactionRepository;
            _stripeTransactionRepository = stripeTransactionRepository;
            _payPalTransactionRepository = payPalTransactionRepository;
        }

        public async Task<PagedResultDto<PaymentTransactionDto>> GetListAsync(GetPaymentTransactionListDto input)
        {
            var queryable = await _p24TransactionRepository.GetQueryableAsync();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                queryable = queryable.Where(x =>
                    x.Email.Contains(input.Filter) ||
                    x.Description.Contains(input.Filter) ||
                    x.SessionId.Contains(input.Filter));
            }

            if (!string.IsNullOrWhiteSpace(input.Status))
            {
                queryable = queryable.Where(x => x.Status == input.Status);
            }

            if (input.StartDate.HasValue)
            {
                queryable = queryable.Where(x => x.CreationTime >= input.StartDate.Value);
            }

            if (input.EndDate.HasValue)
            {
                queryable = queryable.Where(x => x.CreationTime <= input.EndDate.Value);
            }

            if (input.MinAmount.HasValue)
            {
                queryable = queryable.Where(x => x.Amount >= input.MinAmount.Value);
            }

            if (input.MaxAmount.HasValue)
            {
                queryable = queryable.Where(x => x.Amount <= input.MaxAmount.Value);
            }

            if (input.RentalId.HasValue)
            {
                queryable = queryable.Where(x => x.RentalId == input.RentalId.Value);
            }

            if (!string.IsNullOrWhiteSpace(input.Email))
            {
                queryable = queryable.Where(x => x.Email.Contains(input.Email));
            }

            // Apply sorting
            if (!string.IsNullOrWhiteSpace(input.Sorting))
            {
                // Basic sorting to avoid errors - you can extend this as needed
                if (input.Sorting.Contains("CreationTime"))
                {
                    queryable = input.Sorting.Contains("desc")
                        ? queryable.OrderByDescending(x => x.CreationTime)
                        : queryable.OrderBy(x => x.CreationTime);
                }
                else if (input.Sorting.Contains("Amount"))
                {
                    queryable = input.Sorting.Contains("desc")
                        ? queryable.OrderByDescending(x => x.Amount)
                        : queryable.OrderBy(x => x.Amount);
                }
                else if (input.Sorting.Contains("Email"))
                {
                    queryable = input.Sorting.Contains("desc")
                        ? queryable.OrderByDescending(x => x.Email)
                        : queryable.OrderBy(x => x.Email);
                }
                else
                {
                    queryable = queryable.OrderByDescending(x => x.CreationTime);
                }
            }
            else
            {
                queryable = queryable.OrderByDescending(x => x.CreationTime);
            }

            // Get total count
            var totalCount = await AsyncExecuter.CountAsync(queryable);

            // Get paged results
            var items = await AsyncExecuter.ToListAsync(
                queryable.Skip(input.SkipCount).Take(input.MaxResultCount));

            var dtos = ObjectMapper.Map<List<P24Transaction>, List<PaymentTransactionDto>>(items);

            // Set computed properties
            foreach (var dto in dtos)
            {
                dto.StatusDisplayName = GetStatusDisplayName(dto.Status);
                dto.IsCompleted = IsCompletedStatus(dto.Status);
                dto.IsFailed = IsFailedStatus(dto.Status);
                dto.IsPending = IsPendingStatus(dto.Status);
                dto.FormattedAmount = dto.Amount.ToString("N2") + " " + dto.Currency;
                dto.FormattedCreatedAt = dto.CreationTime.ToString("yyyy-MM-dd HH:mm");
                dto.FormattedCompletedAt = dto.LastStatusCheck?.ToString("yyyy-MM-dd HH:mm");
            }

            return new PagedResultDto<PaymentTransactionDto>(totalCount, dtos);
        }

        [AllowAnonymous]
        public async Task<PaymentSuccessViewModel> GetPaymentSuccessViewModelAsync(string sessionId)
        {
            // Try to find P24 transaction first
            var p24Transaction = await _customP24TransactionRepository.FindBySessionIdAsync(sessionId);

            if (p24Transaction != null)
            {
                return await CreatePaymentSuccessViewModelFromP24Async(p24Transaction);
            }

            // Try to find Stripe transaction
            var stripeTransaction = await _stripeTransactionRepository.FindBySessionIdAsync(sessionId);

            if (stripeTransaction != null)
            {
                return await CreatePaymentSuccessViewModelFromStripeAsync(stripeTransaction);
            }

            // Try to find PayPal transaction
            var payPalTransaction = await _payPalTransactionRepository.FindBySessionIdAsync(sessionId);

            if (payPalTransaction != null)
            {
                return await CreatePaymentSuccessViewModelFromPayPalAsync(payPalTransaction);
            }

            // Fallback: Try to find rental by extracting rental ID from sessionId
            // SessionId format: rental_{shortGuid}_{timestamp} or rental_{fullGuid}_{timestamp}
            Logger.LogWarning("No transaction found for sessionId: {SessionId}. Attempting fallback lookup...", sessionId);

            var fallbackRental = await TryFallbackLookupAsync(sessionId);
            if (fallbackRental != null)
            {
                // Create a synthetic transaction response from rental data
                Logger.LogInformation("Fallback lookup successful for rental {RentalId}", fallbackRental.Id);
                return CreatePaymentSuccessViewModelFromRental(fallbackRental);
            }

            throw new BusinessException("MP:PaymentTransactionNotFound")
                .WithData("sessionId", sessionId);
        }

        private async Task<PaymentSuccessViewModel> CreatePaymentSuccessViewModelFromP24Async(P24Transaction transaction)
        {
            var transactionDto = ObjectMapper.Map<P24Transaction, PaymentTransactionDto>(transaction);

            // Set computed properties
            transactionDto.StatusDisplayName = GetStatusDisplayName(transactionDto.Status);
            transactionDto.IsCompleted = IsCompletedStatus(transactionDto.Status);
            transactionDto.IsFailed = IsFailedStatus(transactionDto.Status);
            transactionDto.IsPending = IsPendingStatus(transactionDto.Status);
            transactionDto.FormattedAmount = transactionDto.Amount.ToString("N2") + " " + transactionDto.Currency;
            transactionDto.FormattedCreatedAt = transactionDto.CreationTime.ToString("yyyy-MM-dd HH:mm");
            transactionDto.FormattedCompletedAt = transactionDto.LastStatusCheck?.ToString("yyyy-MM-dd HH:mm");

            // Get related rentals
            var rentals = await GetRentalsFromTransactionAsync(transaction.RentalId, transaction.ExtraProperties);

            var viewModel = new PaymentSuccessViewModel
            {
                Transaction = transactionDto,
                Rentals = rentals,
                Success = transactionDto.IsCompleted,
                Message = GetSuccessMessage(transactionDto.Status),
                NextStepUrl = "/rentals/my-rentals",
                NextStepText = L["PaymentSuccess:ViewMyRentals"],
                TotalAmount = transactionDto.Amount,
                Currency = transactionDto.Currency,
                PaymentDate = transactionDto.LastStatusCheck ?? transactionDto.CreationTime,
                PaymentMethod = "Przelewy24",
                FormattedPaymentDate = (transactionDto.LastStatusCheck ?? transactionDto.CreationTime).ToString("yyyy-MM-dd HH:mm"),
                FormattedTotalAmount = transactionDto.Amount.ToString("N2") + " " + transactionDto.Currency,
                OrderId = transactionDto.OrderId,
                PaymentProvider = "Przelewy24",
                IsVerified = transactionDto.Verified,
                Method = transactionDto.Method
            };

            return viewModel;
        }

        private async Task<PaymentSuccessViewModel> CreatePaymentSuccessViewModelFromStripeAsync(StripeTransaction transaction)
        {
            // Map Stripe transaction to PaymentTransactionDto for consistency
            var transactionDto = new PaymentTransactionDto
            {
                Id = transaction.Id,
                SessionId = transaction.PaymentIntentId, // Checkout Session ID
                Amount = transaction.Amount,
                Currency = transaction.Currency.ToUpperInvariant(),
                Email = transaction.Email,
                Status = MapStripeStatus(transaction.Status),
                StatusDisplayName = GetStripeStatusDisplayName(transaction.Status),
                IsCompleted = transaction.Status == "succeeded",
                IsFailed = transaction.Status == "canceled" || transaction.Status == "requires_payment_method",
                IsPending = transaction.Status == "processing" || transaction.Status == "requires_confirmation",
                FormattedAmount = transaction.Amount.ToString("N2") + " " + transaction.Currency.ToUpperInvariant(),
                CreationTime = transaction.CreationTime,
                FormattedCreatedAt = transaction.CreationTime.ToString("yyyy-MM-dd HH:mm"),
                FormattedCompletedAt = transaction.CompletedAt?.ToString("yyyy-MM-dd HH:mm"),
                Verified = transaction.Status == "succeeded",
                Method = transaction.PaymentMethodType,
                OrderId = transaction.ChargeId
            };

            // Get related rentals from StripeMetadata
            var rentals = await GetRentalsFromStripeMetadataAsync(transaction);

            var viewModel = new PaymentSuccessViewModel
            {
                Transaction = transactionDto,
                Rentals = rentals,
                Success = transactionDto.IsCompleted,
                Message = GetSuccessMessage(MapStripeStatus(transaction.Status)),
                NextStepUrl = "/rentals/my-rentals",
                NextStepText = L["PaymentSuccess:ViewMyRentals"],
                TotalAmount = transactionDto.Amount,
                Currency = transactionDto.Currency,
                PaymentDate = transaction.CompletedAt ?? transaction.CreationTime,
                PaymentMethod = "Stripe",
                FormattedPaymentDate = (transaction.CompletedAt ?? transaction.CreationTime).ToString("yyyy-MM-dd HH:mm"),
                FormattedTotalAmount = transaction.Amount.ToString("N2") + " " + transaction.Currency.ToUpperInvariant(),
                OrderId = transaction.ChargeId,
                PaymentProvider = "Stripe",
                IsVerified = transaction.Status == "succeeded",
                Method = transaction.PaymentMethodType
            };

            return viewModel;
        }

        private async Task<List<RentalDto>> GetRentalsFromTransactionAsync(Guid? rentalId, string? extraProperties)
        {
            var rentals = new List<RentalDto>();

            if (rentalId.HasValue)
            {
                var rental = await _rentalRepository.GetAsync(rentalId.Value);
                rentals.Add(ObjectMapper.Map<Rental, RentalDto>(rental));
            }
            else if (!string.IsNullOrEmpty(extraProperties))
            {
                // For cart checkout, check ExtraProperties for rental IDs
                try
                {
                    var extraProps = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(extraProperties);
                    if (extraProps?.ContainsKey("RentalIds") == true)
                    {
                        var rentalIds = extraProps["RentalIds"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var rentalIdStr in rentalIds)
                        {
                            if (Guid.TryParse(rentalIdStr.Trim(), out var id))
                            {
                                var rental = await _rentalRepository.GetAsync(id);
                                rentals.Add(ObjectMapper.Map<Rental, RentalDto>(rental));
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore JSON parsing errors
                }
            }

            return rentals;
        }

        private async Task<List<RentalDto>> GetRentalsFromStripeMetadataAsync(StripeTransaction transaction)
        {
            var rentals = new List<RentalDto>();

            if (transaction.RentalId.HasValue)
            {
                var rental = await _rentalRepository.GetAsync(transaction.RentalId.Value);
                rentals.Add(ObjectMapper.Map<Rental, RentalDto>(rental));
            }
            else if (!string.IsNullOrEmpty(transaction.StripeMetadata))
            {
                // Parse StripeMetadata JSON to find rental IDs
                try
                {
                    var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(transaction.StripeMetadata);
                    if (metadata?.ContainsKey("rentalIds") == true)
                    {
                        var rentalIdsStr = metadata["rentalIds"].ToString();
                        var rentalIds = rentalIdsStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var rentalIdStr in rentalIds)
                        {
                            if (Guid.TryParse(rentalIdStr.Trim(), out var id))
                            {
                                var rental = await _rentalRepository.GetAsync(id);
                                rentals.Add(ObjectMapper.Map<Rental, RentalDto>(rental));
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore JSON parsing errors
                }
            }

            return rentals;
        }

        private string MapStripeStatus(string stripeStatus)
        {
            // Map Stripe statuses to internal payment statuses
            return stripeStatus switch
            {
                "succeeded" => "completed",
                "processing" => "processing",
                "requires_payment_method" => "pending",
                "requires_confirmation" => "pending",
                "canceled" => "failed",
                _ => "pending"
            };
        }

        private string GetStripeStatusDisplayName(string stripeStatus)
        {
            return stripeStatus switch
            {
                "succeeded" => L["PaymentStatus:Completed"],
                "processing" => L["PaymentStatus:Processing"],
                "requires_payment_method" => L["PaymentStatus:Pending"],
                "requires_confirmation" => L["PaymentStatus:Pending"],
                "canceled" => L["PaymentStatus:Failed"],
                _ => L["PaymentStatus:Unknown"]
            };
        }

        private async Task<PaymentSuccessViewModel> CreatePaymentSuccessViewModelFromPayPalAsync(PayPalTransaction transaction)
        {
            // Map PayPal transaction to PaymentTransactionDto for consistency
            var transactionDto = new PaymentTransactionDto
            {
                Id = transaction.Id,
                SessionId = transaction.OrderId, // PayPal Order ID
                Amount = transaction.Amount,
                Currency = transaction.Currency.ToUpperInvariant(),
                Email = transaction.Email,
                Status = MapPayPalStatus(transaction.Status),
                StatusDisplayName = GetPayPalStatusDisplayName(transaction.Status),
                IsCompleted = transaction.Status == "COMPLETED",
                IsFailed = transaction.Status == "VOIDED" || transaction.Status == "PAYER_ACTION_REQUIRED",
                IsPending = transaction.Status == "CREATED" || transaction.Status == "APPROVED",
                FormattedAmount = transaction.Amount.ToString("N2") + " " + transaction.Currency.ToUpperInvariant(),
                CreationTime = transaction.CreationTime,
                FormattedCreatedAt = transaction.CreationTime.ToString("yyyy-MM-dd HH:mm"),
                FormattedCompletedAt = transaction.CompletedAt?.ToString("yyyy-MM-dd HH:mm"),
                Verified = transaction.Status == "COMPLETED",
                Method = transaction.FundingSource,
                OrderId = transaction.CaptureId ?? transaction.OrderId
            };

            // Get related rentals from PayPalMetadata
            var rentals = await GetRentalsFromPayPalMetadataAsync(transaction);

            var viewModel = new PaymentSuccessViewModel
            {
                Transaction = transactionDto,
                Rentals = rentals,
                Success = transactionDto.IsCompleted,
                Message = GetSuccessMessage(MapPayPalStatus(transaction.Status)),
                NextStepUrl = "/rentals/my-rentals",
                NextStepText = L["PaymentSuccess:ViewMyRentals"],
                TotalAmount = transactionDto.Amount,
                Currency = transactionDto.Currency,
                PaymentDate = transaction.CompletedAt ?? transaction.CreationTime,
                PaymentMethod = "PayPal",
                FormattedPaymentDate = (transaction.CompletedAt ?? transaction.CreationTime).ToString("yyyy-MM-dd HH:mm"),
                FormattedTotalAmount = transaction.Amount.ToString("N2") + " " + transaction.Currency.ToUpperInvariant(),
                OrderId = transaction.CaptureId ?? transaction.OrderId,
                PaymentProvider = "PayPal",
                IsVerified = transaction.Status == "COMPLETED",
                Method = transaction.FundingSource
            };

            return viewModel;
        }

        private async Task<List<RentalDto>> GetRentalsFromPayPalMetadataAsync(PayPalTransaction transaction)
        {
            var rentals = new List<RentalDto>();

            if (transaction.RentalId.HasValue)
            {
                var rental = await _rentalRepository.GetAsync(transaction.RentalId.Value);
                rentals.Add(ObjectMapper.Map<Rental, RentalDto>(rental));
            }
            else if (!string.IsNullOrEmpty(transaction.PayPalMetadata))
            {
                // Parse PayPalMetadata JSON to find rental IDs
                try
                {
                    var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(transaction.PayPalMetadata);
                    if (metadata?.ContainsKey("rentalIds") == true)
                    {
                        var rentalIdsStr = metadata["rentalIds"].ToString();
                        var rentalIds = rentalIdsStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var rentalIdStr in rentalIds)
                        {
                            if (Guid.TryParse(rentalIdStr.Trim(), out var id))
                            {
                                var rental = await _rentalRepository.GetAsync(id);
                                rentals.Add(ObjectMapper.Map<Rental, RentalDto>(rental));
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore JSON parsing errors
                }
            }

            return rentals;
        }

        private string MapPayPalStatus(string payPalStatus)
        {
            // Map PayPal statuses to internal payment statuses
            return payPalStatus switch
            {
                "COMPLETED" => "completed",
                "APPROVED" => "processing",
                "CREATED" => "pending",
                "VOIDED" => "failed",
                "PAYER_ACTION_REQUIRED" => "pending",
                _ => "pending"
            };
        }

        private string GetPayPalStatusDisplayName(string payPalStatus)
        {
            return payPalStatus switch
            {
                "COMPLETED" => L["PaymentStatus:Completed"],
                "APPROVED" => L["PaymentStatus:Processing"],
                "CREATED" => L["PaymentStatus:Pending"],
                "VOIDED" => L["PaymentStatus:Failed"],
                "PAYER_ACTION_REQUIRED" => L["PaymentStatus:Pending"],
                _ => L["PaymentStatus:Unknown"]
            };
        }

        public async Task<PaymentTransactionDto> GetAsync(Guid id)
        {
            var transaction = await _p24TransactionRepository.GetAsync(id);
            var dto = ObjectMapper.Map<P24Transaction, PaymentTransactionDto>(transaction);

            // Set computed properties
            dto.StatusDisplayName = GetStatusDisplayName(dto.Status);
            dto.IsCompleted = IsCompletedStatus(dto.Status);
            dto.IsFailed = IsFailedStatus(dto.Status);
            dto.IsPending = IsPendingStatus(dto.Status);
            dto.FormattedAmount = dto.Amount.ToString("N2") + " " + dto.Currency;
            dto.FormattedCreatedAt = dto.CreationTime.ToString("yyyy-MM-dd HH:mm");
            dto.FormattedCompletedAt = dto.LastStatusCheck?.ToString("yyyy-MM-dd HH:mm");

            return dto;
        }

        public async Task<PaymentTransactionDto> CreateAsync(CreatePaymentTransactionDto input)
        {
            var transaction = new P24Transaction(
                GuidGenerator.Create(),
                input.SessionId,
                input.MerchantId,
                input.PosId,
                input.Amount,
                input.Currency,
                input.Email,
                input.Description,
                input.Sign,
                CurrentTenant.Id);

            transaction.Method = input.Method;
            transaction.TransferLabel = input.TransferLabel;
            transaction.OrderId = input.OrderId;
            transaction.ReturnUrl = input.ReturnUrl;
            transaction.Statement = input.Statement;
            transaction.ExtraProperties = input.ExtraProperties;
            transaction.RentalId = input.RentalId;

            await _p24TransactionRepository.InsertAsync(transaction);

            return ObjectMapper.Map<P24Transaction, PaymentTransactionDto>(transaction);
        }

        public async Task<PaymentTransactionDto> UpdateAsync(Guid id, UpdatePaymentTransactionDto input)
        {
            var transaction = await _p24TransactionRepository.GetAsync(id);

            transaction.Description = input.Description;
            transaction.LastStatusCheck = input.LastStatusCheck;
            transaction.Method = input.Method;
            transaction.TransferLabel = input.TransferLabel;
            transaction.ReturnUrl = input.ReturnUrl;
            transaction.Statement = input.Statement;
            transaction.ExtraProperties = input.ExtraProperties;

            if (input.Status != null)
            {
                transaction.SetStatus(input.Status);
            }

            if (input.Verified.HasValue)
            {
                transaction.SetVerified(input.Verified.Value);
            }

            await _p24TransactionRepository.UpdateAsync(transaction);

            return ObjectMapper.Map<P24Transaction, PaymentTransactionDto>(transaction);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _p24TransactionRepository.DeleteAsync(id);
        }

        private string GetStatusDisplayName(string status)
        {
            return status switch
            {
                "completed" => L["PaymentStatus:Completed"],
                "paid" => L["PaymentStatus:Paid"],
                "pending" => L["PaymentStatus:Pending"],
                "processing" => L["PaymentStatus:Processing"],
                "failed" => L["PaymentStatus:Failed"],
                "cancelled" => L["PaymentStatus:Cancelled"],
                "rejected" => L["PaymentStatus:Rejected"],
                "refunded" => L["PaymentStatus:Refunded"],
                _ => L["PaymentStatus:Unknown"]
            };
        }

        private bool IsCompletedStatus(string status)
        {
            return status is "completed" or "paid";
        }

        private bool IsFailedStatus(string status)
        {
            return status is "failed" or "cancelled" or "rejected";
        }

        private bool IsPendingStatus(string status)
        {
            return status is "pending" or "processing";
        }

        private string GetSuccessMessage(string status)
        {
            return status switch
            {
                "completed" => L["PaymentSuccess:MessageCompleted"],
                "paid" => L["PaymentSuccess:MessagePaid"],
                "processing" => L["PaymentSuccess:MessageProcessing"],
                _ => L["PaymentSuccess:MessagePending"]
            };
        }

        private async Task<Rental?> TryFallbackLookupAsync(string sessionId)
        {
            try
            {
                // SessionId formats:
                // Old: rental_{fullGuid}_{timestamp}
                // New: rental_{shortGuid}_{timestamp}
                // Cart: cart_{shortGuid}_{itemcount}_{timestamp}

                if (!sessionId.StartsWith("rental_", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogDebug("SessionId does not start with 'rental_': {SessionId}", sessionId);
                    return null;
                }

                // Extract the rental ID portion between "rental_" and the timestamp
                var parts = sessionId.Split('_');
                if (parts.Length < 3)
                {
                    Logger.LogDebug("SessionId format invalid (expected at least 3 parts): {SessionId}", sessionId);
                    return null;
                }

                // Try to parse as GUID (short 8-char or full GUID)
                var guidPart = parts[1];

                // Try parsing as full GUID
                if (Guid.TryParse(guidPart, out var fullGuid))
                {
                    Logger.LogDebug("Found full GUID in sessionId: {Guid}", fullGuid);
                    try
                    {
                        var rental = await _rentalRepository.GetAsync(fullGuid);
                        if (rental != null)
                        {
                            Logger.LogInformation("Found rental by full GUID: {RentalId}", fullGuid);
                            return rental;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug(ex, "Failed to lookup rental by full GUID: {Guid}", fullGuid);
                    }
                }

                // Try to find rental by partial GUID match (first 8 chars)
                var shortGuid = guidPart;
                if (shortGuid.Length <= 8)
                {
                    Logger.LogDebug("Attempting partial GUID lookup with: {ShortGuid}", shortGuid);
                    var queryable = await _rentalRepository.GetQueryableAsync();
                    var rentals = await AsyncExecuter.ToListAsync(
                        queryable.Where(r => r.Id.ToString("N").StartsWith(shortGuid))
                    );

                    if (rentals.Count == 1)
                    {
                        Logger.LogInformation("Found rental by partial GUID match: {RentalId}", rentals[0].Id);
                        return rentals[0];
                    }
                    else if (rentals.Count > 1)
                    {
                        Logger.LogWarning("Multiple rentals found for partial GUID {ShortGuid}: {Count} matches", shortGuid, rentals.Count);
                    }
                }

                Logger.LogDebug("No rental found for sessionId: {SessionId}", sessionId);
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during fallback lookup for sessionId: {SessionId}", sessionId);
                return null;
            }
        }

        private PaymentSuccessViewModel CreatePaymentSuccessViewModelFromRental(Rental rental)
        {
            try
            {
                var rentalDto = ObjectMapper.Map<Rental, RentalDto>(rental);

                // Currency is stored on the RentalDto after mapping, default to PLN if not available
                var currency = rentalDto.Currency ?? "PLN";

                var viewModel = new PaymentSuccessViewModel
                {
                    Transaction = new PaymentTransactionDto
                    {
                        Id = Guid.NewGuid(),
                        SessionId = "fallback",
                        Amount = rental.Payment.TotalAmount,
                        Currency = currency,
                        Email = rental.User?.Email ?? "",
                        Status = rental.Payment.IsPaid ? "completed" : "processing",
                        StatusDisplayName = rental.Payment.IsPaid ? L["PaymentStatus:Completed"] : L["PaymentStatus:Processing"],
                        IsCompleted = rental.Payment.IsPaid,
                        IsFailed = false,
                        IsPending = !rental.Payment.IsPaid,
                        FormattedAmount = rental.Payment.TotalAmount.ToString("N2") + " " + currency,
                        CreationTime = rental.CreationTime,
                        FormattedCreatedAt = rental.CreationTime.ToString("yyyy-MM-dd HH:mm"),
                        Verified = rental.Payment.IsPaid
                    },
                    Rentals = new List<RentalDto> { rentalDto },
                    Success = rental.Payment.IsPaid,
                    Message = rental.Payment.IsPaid ? L["PaymentSuccess:MessageCompleted"] : L["PaymentSuccess:MessageProcessing"],
                    NextStepUrl = "/rentals/my-rentals",
                    NextStepText = L["PaymentSuccess:ViewMyRentals"],
                    TotalAmount = rental.Payment.TotalAmount,
                    Currency = currency,
                    PaymentDate = rental.CreationTime,
                    PaymentMethod = "Przelewy24",
                    FormattedPaymentDate = rental.CreationTime.ToString("yyyy-MM-dd HH:mm"),
                    FormattedTotalAmount = rental.Payment.TotalAmount.ToString("N2") + " " + currency,
                    PaymentProvider = "Przelewy24",
                    IsVerified = rental.Payment.IsPaid
                };

                Logger.LogInformation("Created payment success view model from fallback rental lookup for rental {RentalId}", rental.Id);
                return viewModel;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating payment success view model from rental: {RentalId}", rental.Id);
                throw;
            }
        }
    }
}