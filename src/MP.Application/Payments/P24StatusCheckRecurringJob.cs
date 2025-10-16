using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using MP.Domain.Payments;
using MP.Domain.Payments.Events;
using MP.Domain.Rentals;
using MP.Domain.Booths;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Data;

namespace MP.Application.Payments
{
    /// <summary>
    /// Hangfire recurring job that checks payment status for unverified transactions
    /// Runs every 15 minutes
    /// </summary>
    public class P24StatusCheckRecurringJob : ITransientDependency
    {
        private readonly IP24TransactionRepository _p24TransactionRepository;
        private readonly IPrzelewy24Service _przelewy24Service;
        private readonly ILogger<P24StatusCheckRecurringJob> _logger;
        private readonly IRepository<Rental, Guid> _rentalRepository;
        private readonly IBoothRepository _boothRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly ICurrentTenant _currentTenant;
        private readonly IDataFilter<IMultiTenant> _dataFilter;
        private readonly ILocalEventBus _localEventBus;

        public P24StatusCheckRecurringJob(
            IP24TransactionRepository p24TransactionRepository,
            IPrzelewy24Service przelewy24Service,
            ILogger<P24StatusCheckRecurringJob> logger,
            IRepository<Rental, Guid> rentalRepository,
            IBoothRepository boothRepository,
            IUnitOfWorkManager unitOfWorkManager,
            ICurrentTenant currentTenant,
            IDataFilter<IMultiTenant> dataFilter,
            ILocalEventBus localEventBus)
        {
            _p24TransactionRepository = p24TransactionRepository;
            _przelewy24Service = przelewy24Service;
            _logger = logger;
            _rentalRepository = rentalRepository;
            _boothRepository = boothRepository;
            _unitOfWorkManager = unitOfWorkManager;
            _currentTenant = currentTenant;
            _dataFilter = dataFilter;
            _localEventBus = localEventBus;
        }

        [AutomaticRetry(Attempts = 3)]
        public async Task ExecuteAsync()
        {
            _logger.LogInformation("[Hangfire] Starting P24 status check recurring job");

            // Use Unit of Work to ensure changes are saved to database
            using (var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true))
            {
                try
                {
                    List<P24Transaction> transactions;

                    // Disable multi-tenant filter to get transactions from ALL tenants
                    using (_dataFilter.Disable())
                    {
                        _logger.LogInformation("[Hangfire] Multi-tenant filter disabled, fetching transactions from all tenants");

                        // Get transactions that need status checking (ManualStatusCheckCount < 3 and Verified = false)
                        transactions = await _p24TransactionRepository.GetTransactionsForStatusCheckAsync(maxCheckCount: 3);
                    }

                    _logger.LogInformation("[Hangfire] Found {TransactionCount} transactions to check", transactions.Count);

                    foreach (var transaction in transactions)
                    {
                        _logger.LogInformation("[Hangfire] Processing transaction {SessionId} for Tenant {TenantId}",
                            transaction.SessionId, transaction.TenantId);

                        // Process each transaction in its own tenant context
                        using (_currentTenant.Change(transaction.TenantId))
                        {
                            await CheckTransactionStatus(transaction);
                        }
                    }

                    // Commit the unit of work to save changes
                    await uow.CompleteAsync();

                    _logger.LogInformation("[Hangfire] P24 status check recurring job completed. Checked {TransactionCount} transactions", transactions.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Hangfire] Error during P24 status check recurring job");
                    throw;
                }
            }
        }

        private async Task CheckTransactionStatus(P24Transaction transaction)
        {
            try
            {
                if (string.IsNullOrEmpty(transaction.SessionId))
                {
                    _logger.LogWarning("[Hangfire] Transaction {TransactionId} has no SessionId, skipping status check", transaction.Id);
                    transaction.IncrementStatusCheckCount();
                    await _p24TransactionRepository.UpdateAsync(transaction);
                    return;
                }

                _logger.LogInformation("[Hangfire] Checking status for transaction {SessionId}, OrderId: {OrderId}, Check count: {CheckCount}, Current Status: {CurrentStatus}, Verified: {Verified}",
                    transaction.SessionId, transaction.OrderId, transaction.ManualStatusCheckCount, transaction.Status, transaction.Verified);

                // Call Przelewy24 API: GET /api/v1/transaction/by/sessionId/<SessionId>
                var status = await _przelewy24Service.GetPaymentStatusAsync(transaction.SessionId);

                _logger.LogInformation("[Hangfire] P24 API returned status: {P24Status}, Amount: {Amount}",
                    status.Status, status.Amount);

                if (status.Status == "completed")
                {
                    transaction.SetStatus("completed");
                    transaction.SetVerified(true);
                    _logger.LogInformation("[Hangfire] Transaction {SessionId} marked as completed and verified", transaction.SessionId);

                    // Update rentals and booths after successful payment verification
                    await UpdateRentalsAndBoothsAfterPaymentAsync(transaction);
                }
                else if (status.Status == "failed" || status.Status == "cancelled")
                {
                    transaction.SetStatus(status.Status);
                    _logger.LogInformation("[Hangfire] Transaction {SessionId} marked as {Status}", transaction.SessionId, status.Status);
                }
                else
                {
                    _logger.LogInformation("[Hangfire] Transaction {SessionId} still in status: {Status}, keeping as pending", transaction.SessionId, status.Status);
                }

                transaction.IncrementStatusCheckCount();
                await _p24TransactionRepository.UpdateAsync(transaction);

                _logger.LogInformation("[Hangfire] Transaction {SessionId} updated: Status={Status}, Verified={Verified}, CheckCount={CheckCount}",
                    transaction.SessionId, transaction.Status, transaction.Verified, transaction.ManualStatusCheckCount);

                // Check if we've reached the maximum number of status checks
                await HandleMaxStatusChecksReached(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Hangfire] Error checking status for transaction {SessionId}", transaction.SessionId);

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
                    _logger.LogWarning("[Hangfire] Transaction {SessionId} reached max status checks ({Count}) without completion. Releasing booths and cancelling rentals.",
                        transaction.SessionId, transaction.ManualStatusCheckCount);

                    // Get all rentals associated with this transaction via SessionId
                    var rentals = await _rentalRepository.GetListAsync(r =>
                        r.Payment.Przelewy24TransactionId == transaction.SessionId);

                    if (rentals.Count == 0)
                    {
                        _logger.LogWarning("[Hangfire] No rentals found for transaction {SessionId}", transaction.SessionId);
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

                        _logger.LogInformation("[Hangfire] Released booth {BoothId} and cancelled rental {RentalId} due to payment timeout",
                            booth.Id, rental.Id);
                    }

                    _logger.LogInformation("[Hangfire] Cancelled {Count} rentals for transaction {SessionId}",
                        rentals.Count, transaction.SessionId);

                    // Publish PaymentFailed event for notification
                    var firstRental = rentals.FirstOrDefault();
                    if (firstRental != null)
                    {
                        await _localEventBus.PublishAsync(new PaymentFailedEvent
                        {
                            UserId = firstRental.UserId,
                            TransactionId = transaction.SessionId,
                            Amount = transaction.Amount,
                            Currency = transaction.Currency,
                            Reason = "Payment not completed within allowed time",
                            RentalIds = rentals.Select(r => r.Id).ToList(),
                            FailedAt = DateTime.UtcNow
                        });

                        _logger.LogInformation("[Hangfire] Published PaymentFailedEvent for user {UserId}, transaction {SessionId}",
                            firstRental.UserId, transaction.SessionId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Hangfire] Error releasing booths for transaction {SessionId}", transaction.SessionId);
                }
            }
        }

        private async Task UpdateRentalsAndBoothsAfterPaymentAsync(P24Transaction transaction)
        {
            try
            {
                _logger.LogInformation("[Hangfire] Updating rentals and booths for verified transaction {SessionId}", transaction.SessionId);

                // Get all rentals associated with this transaction via SessionId
                var rentals = await _rentalRepository.GetListAsync(r =>
                    r.Payment.Przelewy24TransactionId == transaction.SessionId);

                if (rentals.Count == 0)
                {
                    _logger.LogWarning("[Hangfire] No rentals found for verified transaction {SessionId}", transaction.SessionId);
                    return;
                }

                var paidDate = DateTime.Now;

                // Process each rental and its booth
                foreach (var rental in rentals)
                {
                    // Skip if already paid
                    if (rental.Payment.IsPaid)
                    {
                        _logger.LogInformation("[Hangfire] Rental {RentalId} already marked as paid, skipping", rental.Id);
                        continue;
                    }

                    // Mark rental as paid - this will trigger ConfirmRental and change status to Active
                    rental.MarkAsPaid(rental.Payment.TotalAmount, paidDate, transaction.SessionId);

                    // Get booth and update its status
                    var booth = await _boothRepository.GetAsync(rental.BoothId);

                    // Only update booth status if it's not in Maintenance
                    if (booth.Status != BoothStatus.Maintenance)
                    {
                        // Check if rental period has started
                        if (rental.Period.StartDate <= DateTime.Today)
                        {
                            booth.MarkAsRented();
                            _logger.LogInformation("[Hangfire] Booth {BoothId} marked as Rented (rental started)", booth.Id);
                        }
                        else
                        {
                            booth.MarkAsReserved();
                            _logger.LogInformation("[Hangfire] Booth {BoothId} marked as Reserved (rental starts {StartDate})",
                                booth.Id, rental.Period.StartDate);
                        }

                        await _boothRepository.UpdateAsync(booth);
                    }
                    else
                    {
                        _logger.LogWarning("[Hangfire] Booth {BoothId} is in Maintenance, status not changed", booth.Id);
                    }

                    await _rentalRepository.UpdateAsync(rental);

                    _logger.LogInformation("[Hangfire] Rental {RentalId} marked as paid and booth {BoothId} updated for transaction {SessionId}",
                        rental.Id, rental.BoothId, transaction.SessionId);
                }

                _logger.LogInformation("[Hangfire] Successfully updated {Count} rental(s) and booth(s) for transaction {SessionId}",
                    rentals.Count, transaction.SessionId);

                // Publish PaymentCompleted event for notification
                var firstRental = rentals.FirstOrDefault();
                if (firstRental != null)
                {
                    await _localEventBus.PublishAsync(new PaymentCompletedEvent
                    {
                        UserId = firstRental.UserId,
                        TransactionId = transaction.SessionId,
                        Amount = transaction.Amount,
                        Currency = transaction.Currency,
                        RentalIds = rentals.Select(r => r.Id).ToList(),
                        CompletedAt = DateTime.UtcNow,
                        PaymentMethod = "Przelewy24"
                    });

                    _logger.LogInformation("[Hangfire] Published PaymentCompletedEvent for user {UserId}, transaction {SessionId}",
                        firstRental.UserId, transaction.SessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Hangfire] Error updating rentals and booths for transaction {SessionId}", transaction.SessionId);
                throw;
            }
        }
    }
}
