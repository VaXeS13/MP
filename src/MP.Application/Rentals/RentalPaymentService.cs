using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;
using Volo.Abp;
using MP.Domain.Rentals;
using MP.Domain.Payments;
using MP.Domain.Booths;
using MP.Rentals;

namespace MP.Application.Rentals
{
    public class RentalPaymentService : ApplicationService
    {
        private readonly IPrzelewy24Service _przelewy24Service;
        private readonly IRepository<Rental, Guid> _rentalRepository;
        private readonly IBoothRepository _boothRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RentalPaymentService> _logger;

        public RentalPaymentService(
            IPrzelewy24Service przelewy24Service,
            IRepository<Rental, Guid> rentalRepository,
            IBoothRepository boothRepository,
            ICurrentUser currentUser,
            IConfiguration configuration,
            ILogger<RentalPaymentService> logger)
        {
            _przelewy24Service = przelewy24Service;
            _rentalRepository = rentalRepository;
            _boothRepository = boothRepository;
            _currentUser = currentUser;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> InitiatePaymentAsync(Guid rentalId)
        {
            var rental = await _rentalRepository.GetAsync(rentalId);

            // Sprawdź czy user jest właścicielem rental
            if (rental.UserId != _currentUser.GetId())
            {
                throw new BusinessException("ACCESS_DENIED");
            }

            // Sprawdź czy rental nie jest już opłacony
            if (rental.Payment.IsPaid)
            {
                throw new BusinessException("RENTAL_ALREADY_PAID");
            }

            // Sprawdź czy rental jest w statusie Draft
            if (rental.Status != RentalStatus.Draft)
            {
                throw new BusinessException("RENTAL_NOT_IN_DRAFT_STATUS");
            }

            try
            {
                var sessionId = $"rental_{rentalId}_{DateTime.Now:yyyyMMddHHmmss}";

                var paymentRequest = new Przelewy24PaymentRequest
                {
                    MerchantId = _configuration["Przelewy24:MerchantId"]!,
                    PosId = _configuration["Przelewy24:PosId"]!,
                    SessionId = sessionId,
                    Amount = rental.Payment.TotalAmount,
                    Description = $"Wypożyczenie stanowiska {rental.BoothId} na okres {rental.Period.StartDate:dd.MM.yyyy} - {rental.Period.EndDate:dd.MM.yyyy}",
                    Email = _currentUser.Email ?? "",
                    ClientName = _currentUser.Name ?? "Klient",
                    UrlReturn = _configuration["App:ClientUrl"] + $"/rentals/payment-success/{sessionId}",
                    UrlStatus = _configuration["App:ApiUrl"] + "/api/rentals/payment-callback"
                };

                var result = await _przelewy24Service.CreatePaymentAsync(paymentRequest);

                if (result.Success)
                {
                    // Zaktualizuj rental z transaction ID
                    rental.Payment.SetTransactionId(result.TransactionId);
                    await _rentalRepository.UpdateAsync(rental);

                    _logger.LogInformation("Payment initiated for rental {RentalId}, transaction {TransactionId}",
                        rentalId, result.TransactionId);

                    return result.PaymentUrl;
                }
                else
                {
                    _logger.LogError("Failed to initiate payment for rental {RentalId}: {Error}",
                        rentalId, result.ErrorMessage);
                    throw new BusinessException("PAYMENT_INITIATION_FAILED", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating payment for rental {RentalId}", rentalId);
                throw;
            }
        }

        public async Task<bool> HandlePaymentCallbackAsync(string transactionId, bool isSuccess)
        {
            try
            {
                // Find ALL rentals associated with this transaction ID (SessionId)
                // This handles both single rental and cart checkout (multiple rentals)
                var rentals = await _rentalRepository.GetListAsync(r =>
                    r.Payment.Przelewy24TransactionId == transactionId);

                if (rentals.Count == 0)
                {
                    _logger.LogWarning("No rentals found for transaction {TransactionId}", transactionId);
                    return false;
                }

                _logger.LogInformation("Found {Count} rental(s) for transaction {TransactionId}",
                    rentals.Count, transactionId);

                if (isSuccess)
                {
                    // Calculate total expected amount for all rentals
                    var totalExpectedAmount = rentals.Sum(r => r.Payment.TotalAmount);

                    // Verify payment in Przelewy24 once for the total amount
                    var isValid = await _przelewy24Service.VerifyPaymentAsync(transactionId, totalExpectedAmount);

                    if (isValid)
                    {
                        var paidDate = DateTime.Now;

                        // Mark all rentals as paid and their booths as rented
                        foreach (var rental in rentals)
                        {
                            // Mark rental as paid
                            rental.MarkAsPaid(rental.Payment.TotalAmount, paidDate, transactionId);

                            // Change booth status from Reserved to Rented
                            var booth = await _boothRepository.GetAsync(rental.BoothId);
                            booth.MarkAsRented();
                            await _boothRepository.UpdateAsync(booth);

                            await _rentalRepository.UpdateAsync(rental);

                            _logger.LogInformation("Payment confirmed for rental {RentalId}. Booth {BoothId} marked as rented.",
                                rental.Id, rental.BoothId);
                        }

                        _logger.LogInformation("Payment confirmed for {Count} rental(s), transaction {TransactionId}",
                            rentals.Count, transactionId);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Payment verification failed for transaction {TransactionId}", transactionId);

                        // Mark all rentals as failed and release their booths
                        foreach (var rental in rentals)
                        {
                            rental.Payment.MarkAsFailed();

                            // Release booth (from Reserved to Available)
                            var booth = await _boothRepository.GetAsync(rental.BoothId);
                            booth.MarkAsAvailable();
                            await _boothRepository.UpdateAsync(booth);

                            await _rentalRepository.UpdateAsync(rental);
                        }

                        return false;
                    }
                }
                else
                {
                    // Payment failed - mark all rentals as failed and release booths
                    foreach (var rental in rentals)
                    {
                        rental.Payment.MarkAsFailed();

                        // Release booth (from Reserved to Available)
                        var booth = await _boothRepository.GetAsync(rental.BoothId);
                        booth.MarkAsAvailable();
                        await _boothRepository.UpdateAsync(booth);

                        await _rentalRepository.UpdateAsync(rental);

                        _logger.LogInformation("Payment failed for rental {RentalId}. Booth {BoothId} released.",
                            rental.Id, rental.BoothId);
                    }

                    _logger.LogInformation("Payment failed for {Count} rental(s), transaction {TransactionId}",
                        rentals.Count, transactionId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling payment callback for transaction {TransactionId}", transactionId);
                return false;
            }
        }

        public async Task<PaymentStatus> GetPaymentStatusAsync(Guid rentalId)
        {
            var rental = await _rentalRepository.GetAsync(rentalId);

            // Sprawdź czy user jest właścicielem rental
            if (rental.UserId != _currentUser.GetId())
            {
                throw new BusinessException("ACCESS_DENIED");
            }

            return rental.Payment.PaymentStatus;
        }
    }
}