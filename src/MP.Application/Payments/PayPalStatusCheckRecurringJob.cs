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
    /// Hangfire recurring job that checks PayPal payment status for pending transactions
    /// Runs every 15 minutes
    /// </summary>
    public class PayPalStatusCheckRecurringJob : ITransientDependency
    {
        private readonly IPayPalTransactionRepository _paypalTransactionRepository;
        private readonly ILogger<PayPalStatusCheckRecurringJob> _logger;
        private readonly IRepository<Rental, Guid> _rentalRepository;
        private readonly IBoothRepository _boothRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly ICurrentTenant _currentTenant;
        private readonly IDataFilter<IMultiTenant> _dataFilter;

        public PayPalStatusCheckRecurringJob(
            IPayPalTransactionRepository paypalTransactionRepository,
            ILogger<PayPalStatusCheckRecurringJob> logger,
            IRepository<Rental, Guid> rentalRepository,
            IBoothRepository boothRepository,
            IUnitOfWorkManager unitOfWorkManager,
            ICurrentTenant currentTenant,
            IDataFilter<IMultiTenant> dataFilter)
        {
            _paypalTransactionRepository = paypalTransactionRepository;
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
            _logger.LogInformation("[Hangfire] Starting PayPal status check recurring job");

            using (var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true))
            {
                try
                {
                    List<PayPalTransaction> transactions;

                    // Disable multi-tenant filter to get transactions from ALL tenants
                    using (_dataFilter.Disable())
                    {
                        _logger.LogInformation("[Hangfire] Multi-tenant filter disabled, fetching PayPal transactions from all tenants");

                        // Get transactions that need status checking
                        // Criteria: StatusCheckCount < 3 AND Status NOT IN ('COMPLETED', 'VOIDED', 'CANCELLED')
                        var cutoffTime = DateTime.UtcNow.AddHours(-1); // Check transactions older than 1 hour
                        transactions = await _paypalTransactionRepository.GetPendingStatusChecksAsync(cutoffTime, maxCount: 100);

                        // Additional filtering for PayPal-specific statuses
                        transactions = transactions
                            .Where(t => t.StatusCheckCount < 3 &&
                                       !t.IsCompleted() &&
                                       !t.IsCancelled())
                            .ToList();
                    }

                    _logger.LogInformation("[Hangfire] Found {TransactionCount} PayPal transactions to check", transactions.Count);

                    foreach (var transaction in transactions)
                    {
                        _logger.LogInformation("[Hangfire] Processing PayPal transaction {OrderId} for Tenant {TenantId}",
                            transaction.OrderId, transaction.TenantId);

                        // Process each transaction in its own tenant context
                        using (_currentTenant.Change(transaction.TenantId))
                        {
                            await CheckTransactionStatus(transaction);
                        }
                    }

                    // Commit the unit of work to save changes
                    await uow.CompleteAsync();

                    _logger.LogInformation("[Hangfire] PayPal status check recurring job completed. Checked {TransactionCount} transactions", transactions.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Hangfire] Error during PayPal status check recurring job");
                    throw;
                }
            }
        }

        private async Task CheckTransactionStatus(PayPalTransaction transaction)
        {
            try
            {
                if (string.IsNullOrEmpty(transaction.OrderId))
                {
                    _logger.LogWarning("[Hangfire] PayPal transaction {TransactionId} has no OrderId, skipping status check", transaction.Id);
                    transaction.IncrementStatusCheckCount();
                    await _paypalTransactionRepository.UpdateAsync(transaction);
                    return;
                }

                _logger.LogInformation("[Hangfire] Checking status for PayPal transaction {OrderId}, Check count: {CheckCount}, Current Status: {CurrentStatus}",
                    transaction.OrderId, transaction.StatusCheckCount, transaction.Status);

                // NOTE: PayPal API integration would go here
                // For now, we'll simulate the status check logic
                // In production, you would call: await _paypalService.GetOrderStatusAsync(transaction.OrderId);

                // Simulated status response - replace with actual API call:
                // var orderDetails = await _paypalService.GetOrderDetailsAsync(transaction.OrderId);
                // string currentStatus = orderDetails.Status;

                string currentStatus = transaction.Status; // Placeholder - replace with API response

                _logger.LogInformation("[Hangfire] PayPal API would return status: {Status} for Order {OrderId}",
                    currentStatus, transaction.OrderId);

                if (currentStatus == "COMPLETED")
                {
                    transaction.SetStatus("COMPLETED");
                    _logger.LogInformation("[Hangfire] PayPal transaction {OrderId} marked as completed", transaction.OrderId);

                    // Update rentals and booths after successful payment verification
                    await UpdateRentalsAndBoothsAfterPaymentAsync(transaction);
                }
                else if (currentStatus == "VOIDED" || currentStatus == "CANCELLED")
                {
                    transaction.SetStatus(currentStatus);
                    _logger.LogInformation("[Hangfire] PayPal transaction {OrderId} marked as {Status}", transaction.OrderId, currentStatus);
                }
                else if (currentStatus == "APPROVED")
                {
                    // Payment approved but not yet captured - needs manual capture
                    transaction.SetStatus("APPROVED");
                    _logger.LogInformation("[Hangfire] PayPal transaction {OrderId} is approved, waiting for capture", transaction.OrderId);
                }
                else
                {
                    _logger.LogInformation("[Hangfire] PayPal transaction {OrderId} still in status: {Status}, keeping as pending",
                        transaction.OrderId, currentStatus);
                }

                transaction.IncrementStatusCheckCount();
                await _paypalTransactionRepository.UpdateAsync(transaction);

                _logger.LogInformation("[Hangfire] PayPal transaction {OrderId} updated: Status={Status}, CheckCount={CheckCount}",
                    transaction.OrderId, transaction.Status, transaction.StatusCheckCount);

                // Check if we've reached the maximum number of status checks
                await HandleMaxStatusChecksReached(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Hangfire] Error checking status for PayPal transaction {OrderId}", transaction.OrderId);

                // Increment check count even on error to prevent infinite retries
                transaction.IncrementStatusCheckCount();
                await _paypalTransactionRepository.UpdateAsync(transaction);

                // Check if we've reached the maximum number of status checks
                await HandleMaxStatusChecksReached(transaction);
            }
        }

        private async Task HandleMaxStatusChecksReached(PayPalTransaction transaction)
        {
            // Check if we've reached 3 status checks and transaction is not completed
            if (transaction.StatusCheckCount >= 3 && !transaction.IsCompleted())
            {
                try
                {
                    _logger.LogWarning("[Hangfire] PayPal transaction {OrderId} reached max status checks ({Count}) without completion. Releasing booths and cancelling rentals.",
                        transaction.OrderId, transaction.StatusCheckCount);

                    // Get all rentals associated with this transaction via OrderId
                    // PayPal transactions may be linked by storing OrderId in Payment.Przelewy24TransactionId
                    // or by RentalId field in PayPalTransaction
                    List<Rental> rentals = new List<Rental>();

                    if (transaction.RentalId.HasValue)
                    {
                        var rental = await _rentalRepository.GetAsync(transaction.RentalId.Value);
                        rentals.Add(rental);
                    }
                    else
                    {
                        // Fallback: search by OrderId in Payment.Przelewy24TransactionId
                        rentals = await _rentalRepository.GetListAsync(r =>
                            r.Payment.Przelewy24TransactionId == transaction.OrderId);
                    }

                    if (rentals.Count == 0)
                    {
                        _logger.LogWarning("[Hangfire] No rentals found for PayPal transaction {OrderId}", transaction.OrderId);
                        return;
                    }

                    // Cancel all rentals and release their booths
                    foreach (var rental in rentals)
                    {
                        var booth = await _boothRepository.GetAsync(rental.BoothId);

                        // Mark rental as cancelled and booth as available
                        rental.Cancel("PayPal payment not completed within allowed time");
                        booth.MarkAsAvailable();

                        await _rentalRepository.UpdateAsync(rental);
                        await _boothRepository.UpdateAsync(booth);

                        _logger.LogInformation("[Hangfire] Released booth {BoothId} and cancelled rental {RentalId} due to PayPal payment timeout",
                            booth.Id, rental.Id);
                    }

                    _logger.LogInformation("[Hangfire] Cancelled {Count} rentals for PayPal transaction {OrderId}",
                        rentals.Count, transaction.OrderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Hangfire] Error releasing booths for PayPal transaction {OrderId}", transaction.OrderId);
                }
            }
        }

        private async Task UpdateRentalsAndBoothsAfterPaymentAsync(PayPalTransaction transaction)
        {
            try
            {
                _logger.LogInformation("[Hangfire] Updating rentals and booths for verified PayPal transaction {OrderId}", transaction.OrderId);

                // Get all rentals associated with this transaction
                List<Rental> rentals = new List<Rental>();

                if (transaction.RentalId.HasValue)
                {
                    var rental = await _rentalRepository.GetAsync(transaction.RentalId.Value);
                    rentals.Add(rental);
                }
                else
                {
                    // Fallback: search by OrderId in Payment.Przelewy24TransactionId
                    rentals = await _rentalRepository.GetListAsync(r =>
                        r.Payment.Przelewy24TransactionId == transaction.OrderId);
                }

                if (rentals.Count == 0)
                {
                    _logger.LogWarning("[Hangfire] No rentals found for verified PayPal transaction {OrderId}", transaction.OrderId);
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
                    rental.MarkAsPaid(rental.Payment.TotalAmount, paidDate, transaction.OrderId);

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

                    _logger.LogInformation("[Hangfire] Rental {RentalId} marked as paid and booth {BoothId} updated for PayPal transaction {OrderId}",
                        rental.Id, rental.BoothId, transaction.OrderId);
                }

                _logger.LogInformation("[Hangfire] Successfully updated {Count} rental(s) and booth(s) for PayPal transaction {OrderId}",
                    rentals.Count, transaction.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Hangfire] Error updating rentals and booths for PayPal transaction {OrderId}", transaction.OrderId);
                throw;
            }
        }
    }
}
