using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using MP.Domain.Payments;
using MP.Domain.Rentals;
using MP.Domain.Booths;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Data;

namespace MP.Application.Payments
{
    /// <summary>
    /// Hangfire recurring job that checks Stripe payment status for pending transactions
    /// Runs every 15 minutes
    /// </summary>
    public class StripeStatusCheckRecurringJob : ITransientDependency
    {
        private readonly IStripeTransactionRepository _stripeTransactionRepository;
        private readonly ILogger<StripeStatusCheckRecurringJob> _logger;
        private readonly IRepository<Rental, Guid> _rentalRepository;
        private readonly IBoothRepository _boothRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly ICurrentTenant _currentTenant;
        private readonly IDataFilter<IMultiTenant> _dataFilter;

        public StripeStatusCheckRecurringJob(
            IStripeTransactionRepository stripeTransactionRepository,
            ILogger<StripeStatusCheckRecurringJob> logger,
            IRepository<Rental, Guid> rentalRepository,
            IBoothRepository boothRepository,
            IUnitOfWorkManager unitOfWorkManager,
            ICurrentTenant currentTenant,
            IDataFilter<IMultiTenant> dataFilter)
        {
            _stripeTransactionRepository = stripeTransactionRepository;
            _logger = logger;
            _rentalRepository = rentalRepository;
            _boothRepository = boothRepository;
            _unitOfWorkManager = unitOfWorkManager;
            _currentTenant = currentTenant;
            _dataFilter = dataFilter;
        }

        [AutomaticRetry(Attempts = 3)]
        public async Task ExecuteAsync()
        {
            _logger.LogInformation("[Hangfire] Starting Stripe status check recurring job");

            using (var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true))
            {
                try
                {
                    List<StripeTransaction> transactions;

                    // Disable multi-tenant filter to get transactions from ALL tenants
                    using (_dataFilter.Disable())
                    {
                        _logger.LogInformation("[Hangfire] Multi-tenant filter disabled, fetching Stripe transactions from all tenants");

                        // Get transactions that need status checking
                        // Criteria: StatusCheckCount < 3 AND Status NOT IN ('succeeded', 'canceled')
                        var cutoffTime = DateTime.UtcNow.AddHours(-1); // Check transactions older than 1 hour
                        transactions = await _stripeTransactionRepository.GetPendingStatusChecksAsync(cutoffTime, maxCount: 100);

                        // Additional filtering for Stripe-specific statuses
                        transactions = transactions
                            .Where(t => t.StatusCheckCount < 3 &&
                                       !t.IsCompleted() &&
                                       !t.IsFailed())
                            .ToList();
                    }

                    _logger.LogInformation("[Hangfire] Found {TransactionCount} Stripe transactions to check", transactions.Count);

                    foreach (var transaction in transactions)
                    {
                        _logger.LogInformation("[Hangfire] Processing Stripe transaction {PaymentIntentId} for Tenant {TenantId}",
                            transaction.PaymentIntentId, transaction.TenantId);

                        // Process each transaction in its own tenant context
                        using (_currentTenant.Change(transaction.TenantId))
                        {
                            await CheckTransactionStatus(transaction);
                        }
                    }

                    // Commit the unit of work to save changes
                    await uow.CompleteAsync();

                    _logger.LogInformation("[Hangfire] Stripe status check recurring job completed. Checked {TransactionCount} transactions", transactions.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Hangfire] Error during Stripe status check recurring job");
                    throw;
                }
            }
        }

        private async Task CheckTransactionStatus(StripeTransaction transaction)
        {
            try
            {
                if (string.IsNullOrEmpty(transaction.PaymentIntentId))
                {
                    _logger.LogWarning("[Hangfire] Stripe transaction {TransactionId} has no PaymentIntentId, skipping status check", transaction.Id);
                    transaction.IncrementStatusCheckCount();
                    await _stripeTransactionRepository.UpdateAsync(transaction);
                    return;
                }

                _logger.LogInformation("[Hangfire] Checking status for Stripe transaction {PaymentIntentId}, Check count: {CheckCount}, Current Status: {CurrentStatus}",
                    transaction.PaymentIntentId, transaction.StatusCheckCount, transaction.Status);

                // NOTE: Stripe API integration would go here
                // For now, we'll simulate the status check logic
                // In production, you would call: await _stripeService.GetPaymentIntentAsync(transaction.PaymentIntentId);

                // Simulated status response - replace with actual API call:
                // var paymentIntent = await _stripeService.GetPaymentIntentAsync(transaction.PaymentIntentId);
                // string currentStatus = paymentIntent.Status;

                string currentStatus = transaction.Status; // Placeholder - replace with API response

                _logger.LogInformation("[Hangfire] Stripe API would return status: {Status} for PaymentIntent {PaymentIntentId}",
                    currentStatus, transaction.PaymentIntentId);

                if (currentStatus == "succeeded")
                {
                    transaction.SetStatus("succeeded");
                    _logger.LogInformation("[Hangfire] Stripe transaction {PaymentIntentId} marked as succeeded", transaction.PaymentIntentId);

                    // Update rentals and booths after successful payment verification
                    await UpdateRentalsAndBoothsAfterPaymentAsync(transaction);
                }
                else if (currentStatus == "canceled")
                {
                    transaction.SetStatus("canceled");
                    _logger.LogInformation("[Hangfire] Stripe transaction {PaymentIntentId} marked as canceled", transaction.PaymentIntentId);
                }
                else if (currentStatus == "requires_payment_method")
                {
                    // Payment failed and requires new payment method
                    transaction.SetStatus("requires_payment_method");
                    _logger.LogInformation("[Hangfire] Stripe transaction {PaymentIntentId} requires new payment method", transaction.PaymentIntentId);
                }
                else if (currentStatus == "processing")
                {
                    // Payment is being processed (e.g., SEPA debit, ACH)
                    transaction.SetStatus("processing");
                    _logger.LogInformation("[Hangfire] Stripe transaction {PaymentIntentId} is processing, will check again later", transaction.PaymentIntentId);
                }
                else
                {
                    _logger.LogInformation("[Hangfire] Stripe transaction {PaymentIntentId} still in status: {Status}, keeping as pending",
                        transaction.PaymentIntentId, currentStatus);
                }

                transaction.IncrementStatusCheckCount();
                await _stripeTransactionRepository.UpdateAsync(transaction);

                _logger.LogInformation("[Hangfire] Stripe transaction {PaymentIntentId} updated: Status={Status}, CheckCount={CheckCount}",
                    transaction.PaymentIntentId, transaction.Status, transaction.StatusCheckCount);

                // Check if we've reached the maximum number of status checks
                await HandleMaxStatusChecksReached(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Hangfire] Error checking status for Stripe transaction {PaymentIntentId}", transaction.PaymentIntentId);

                // Increment check count even on error to prevent infinite retries
                transaction.IncrementStatusCheckCount();
                await _stripeTransactionRepository.UpdateAsync(transaction);

                // Check if we've reached the maximum number of status checks
                await HandleMaxStatusChecksReached(transaction);
            }
        }

        private async Task HandleMaxStatusChecksReached(StripeTransaction transaction)
        {
            // Check if we've reached 3 status checks and transaction is not completed
            if (transaction.StatusCheckCount >= 3 && !transaction.IsCompleted())
            {
                try
                {
                    _logger.LogWarning("[Hangfire] Stripe transaction {PaymentIntentId} reached max status checks ({Count}) without completion. Releasing booths and cancelling rentals.",
                        transaction.PaymentIntentId, transaction.StatusCheckCount);

                    // Get all rentals associated with this transaction via PaymentIntentId
                    // Stripe transactions may be linked by storing PaymentIntentId in Payment.Przelewy24TransactionId
                    // or by RentalId field in StripeTransaction
                    List<Rental> rentals = new List<Rental>();

                    if (transaction.RentalId.HasValue)
                    {
                        var rental = await _rentalRepository.GetAsync(transaction.RentalId.Value);
                        rentals.Add(rental);
                    }
                    else
                    {
                        // Fallback: search by PaymentIntentId in Payment.Przelewy24TransactionId
                        rentals = await _rentalRepository.GetListAsync(r =>
                            r.Payment.Przelewy24TransactionId == transaction.PaymentIntentId);
                    }

                    if (rentals.Count == 0)
                    {
                        _logger.LogWarning("[Hangfire] No rentals found for Stripe transaction {PaymentIntentId}", transaction.PaymentIntentId);
                        return;
                    }

                    // Cancel all rentals and release their booths
                    foreach (var rental in rentals)
                    {
                        var booth = await _boothRepository.GetAsync(rental.BoothId);

                        // Mark rental as cancelled and booth as available
                        rental.Cancel("Stripe payment not completed within allowed time");
                        booth.MarkAsAvailable();

                        await _rentalRepository.UpdateAsync(rental);
                        await _boothRepository.UpdateAsync(booth);

                        _logger.LogInformation("[Hangfire] Released booth {BoothId} and cancelled rental {RentalId} due to Stripe payment timeout",
                            booth.Id, rental.Id);
                    }

                    _logger.LogInformation("[Hangfire] Cancelled {Count} rentals for Stripe transaction {PaymentIntentId}",
                        rentals.Count, transaction.PaymentIntentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Hangfire] Error releasing booths for Stripe transaction {PaymentIntentId}", transaction.PaymentIntentId);
                }
            }
        }

        private async Task UpdateRentalsAndBoothsAfterPaymentAsync(StripeTransaction transaction)
        {
            try
            {
                _logger.LogInformation("[Hangfire] Updating rentals and booths for verified Stripe transaction {PaymentIntentId}", transaction.PaymentIntentId);

                // Get all rentals associated with this transaction
                List<Rental> rentals = new List<Rental>();

                if (transaction.RentalId.HasValue)
                {
                    var rental = await _rentalRepository.GetAsync(transaction.RentalId.Value);
                    rentals.Add(rental);
                }
                else
                {
                    // Fallback: search by PaymentIntentId in Payment.Przelewy24TransactionId
                    rentals = await _rentalRepository.GetListAsync(r =>
                        r.Payment.Przelewy24TransactionId == transaction.PaymentIntentId);
                }

                if (rentals.Count == 0)
                {
                    _logger.LogWarning("[Hangfire] No rentals found for verified Stripe transaction {PaymentIntentId}", transaction.PaymentIntentId);
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
                    rental.MarkAsPaid(rental.Payment.TotalAmount, paidDate, transaction.PaymentIntentId);

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

                    _logger.LogInformation("[Hangfire] Rental {RentalId} marked as paid and booth {BoothId} updated for Stripe transaction {PaymentIntentId}",
                        rental.Id, rental.BoothId, transaction.PaymentIntentId);
                }

                _logger.LogInformation("[Hangfire] Successfully updated {Count} rental(s) and booth(s) for Stripe transaction {PaymentIntentId}",
                    rentals.Count, transaction.PaymentIntentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Hangfire] Error updating rentals and booths for Stripe transaction {PaymentIntentId}", transaction.PaymentIntentId);
                throw;
            }
        }
    }
}
