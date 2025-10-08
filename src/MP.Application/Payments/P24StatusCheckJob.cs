using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using MP.Domain.Payments;
using MP.Domain.Rentals;
using MP.Domain.Booths;
using Volo.Abp.Domain.Repositories;
using Newtonsoft.Json;

namespace MP.Application.Payments
{
    public class P24StatusCheckJob : AsyncBackgroundJob<P24StatusCheckJobArgs>, ITransientDependency
    {
        private readonly IP24TransactionRepository _p24TransactionRepository;
        private readonly IPrzelewy24Service _przelewy24Service;
        private readonly ILogger<P24StatusCheckJob> _logger;
        private readonly IRepository<Rental, Guid> _rentalRepository;
        private readonly IBoothRepository _boothRepository;

        public P24StatusCheckJob(
            IP24TransactionRepository p24TransactionRepository,
            IPrzelewy24Service przelewy24Service,
            ILogger<P24StatusCheckJob> logger,
            IRepository<Rental, Guid> rentalRepository,
            IBoothRepository boothRepository)
        {
            _p24TransactionRepository = p24TransactionRepository;
            _przelewy24Service = przelewy24Service;
            _logger = logger;
            _rentalRepository = rentalRepository;
            _boothRepository = boothRepository;
        }

        public override async Task ExecuteAsync(P24StatusCheckJobArgs args)
        {
            _logger.LogInformation("Starting P24 status check job");

            try
            {
                // Get transactions that need status checking
                var transactions = await _p24TransactionRepository.GetTransactionsForStatusCheckAsync(maxCheckCount: 3);

                foreach (var transaction in transactions)
                {
                    await CheckTransactionStatus(transaction);
                }

                _logger.LogInformation("P24 status check job completed. Checked {TransactionCount} transactions", transactions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during P24 status check job");
                throw;
            }
        }

        private async Task CheckTransactionStatus(P24Transaction transaction)
        {
            try
            {
                if (string.IsNullOrEmpty(transaction.SessionId))
                {
                    _logger.LogWarning("Transaction has no SessionId, skipping status check");
                    transaction.IncrementStatusCheckCount();
                    await _p24TransactionRepository.UpdateAsync(transaction);
                    return;
                }

                _logger.LogInformation("Checking status for transaction {SessionId}, OrderId: {OrderId}",
                    transaction.SessionId, transaction.OrderId);

                var status = await _przelewy24Service.GetPaymentStatusAsync(transaction.SessionId);

                if (status.Status == "completed")
                {
                    transaction.SetStatus("completed");
                    transaction.SetVerified(true);
                    _logger.LogInformation("Transaction {SessionId} marked as completed", transaction.SessionId);
                }
                else if (status.Status == "failed" || status.Status == "cancelled")
                {
                    transaction.SetStatus(status.Status);
                    _logger.LogInformation("Transaction {SessionId} marked as {Status}", transaction.SessionId, status.Status);
                }
                else
                {
                    _logger.LogInformation("Transaction {SessionId} still in status: {Status}", transaction.SessionId, status.Status);
                }

                transaction.IncrementStatusCheckCount();
                await _p24TransactionRepository.UpdateAsync(transaction);

                // Check if we've reached the maximum number of status checks
                await HandleMaxStatusChecksReached(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking status for transaction {SessionId}", transaction.SessionId);

                // Increment check count even on error to prevent infinite retries
                transaction.IncrementStatusCheckCount();
                await _p24TransactionRepository.UpdateAsync(transaction);

                // Check if we've reached the maximum number of status checks
                await HandleMaxStatusChecksReached(transaction);
            }
        }

        private async Task HandleMaxStatusChecksReached(P24Transaction transaction)
        {
            // Check if we've reached 3 status checks and transaction is not completed
            if (transaction.ManualStatusCheckCount >= 3 && transaction.Status != "completed")
            {
                try
                {
                    _logger.LogWarning("Transaction {SessionId} reached max status checks ({Count}) without completion. Releasing booths and cancelling rentals.",
                        transaction.SessionId, transaction.ManualStatusCheckCount);

                    // Get all rentals associated with this transaction via SessionId
                    var rentals = await _rentalRepository.GetListAsync(r =>
                        r.Payment.Przelewy24TransactionId == transaction.SessionId);

                    if (rentals.Count == 0)
                    {
                        _logger.LogWarning("No rentals found for transaction {SessionId}", transaction.SessionId);
                        return;
                    }

                    // Cancel all rentals and release their booths
                    foreach (var rental in rentals)
                    {
                        var booth = await _boothRepository.GetAsync(rental.BoothId);

                        // Mark rental as cancelled and booth as available
                        rental.Cancel("Payment not completed within allowed time");
                        booth.MarkAsAvailable();

                        await _rentalRepository.UpdateAsync(rental);
                        await _boothRepository.UpdateAsync(booth);

                        _logger.LogInformation("Released booth {BoothId} and cancelled rental {RentalId} due to payment timeout",
                            booth.Id, rental.Id);
                    }

                    _logger.LogInformation("Cancelled {Count} rentals for transaction {SessionId}",
                        rentals.Count, transaction.SessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error releasing booths for transaction {SessionId}", transaction.SessionId);
                }
            }
        }
    }

    public class P24StatusCheckJobArgs
    {
        public DateTime ScheduledTime { get; set; } = DateTime.UtcNow;
    }
}