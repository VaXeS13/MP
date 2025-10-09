using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using MP.Domain.Rentals;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Data;
using MP.Rentals;

namespace MP.Application.Payments
{
    /// <summary>
    /// Hangfire recurring job that synchronizes rental statuses based on end dates
    /// Runs daily at 00:01 to automatically expire rentals that have passed their end date
    /// </summary>
    public class DailyRentalStatusSyncJob : ITransientDependency
    {
        private readonly IRentalRepository _rentalRepository;
        private readonly ILogger<DailyRentalStatusSyncJob> _logger;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly ICurrentTenant _currentTenant;
        private readonly IDataFilter<IMultiTenant> _dataFilter;

        public DailyRentalStatusSyncJob(
            IRentalRepository rentalRepository,
            ILogger<DailyRentalStatusSyncJob> logger,
            IUnitOfWorkManager unitOfWorkManager,
            ICurrentTenant currentTenant,
            IDataFilter<IMultiTenant> dataFilter)
        {
            _rentalRepository = rentalRepository;
            _logger = logger;
            _unitOfWorkManager = unitOfWorkManager;
            _currentTenant = currentTenant;
            _dataFilter = dataFilter;
        }

        [AutomaticRetry(Attempts = 3)]
        public async Task ExecuteAsync()
        {
            _logger.LogInformation("[Hangfire] Starting daily rental status synchronization job");

            using (var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true))
            {
                try
                {
                    int rentalsExpired = 0;
                    var today = DateTime.Today;

                    List<Rental> allRentals;
                    List<Guid?> tenantIds;

                    // Disable multi-tenant filter to process all tenants
                    using (_dataFilter.Disable())
                    {
                        _logger.LogInformation("[Hangfire] Multi-tenant filter disabled, fetching rentals from all tenants");

                        // Get all active or extended rentals that have passed their end date
                        allRentals = (await _rentalRepository.GetQueryableAsync())
                            .Where(r => (r.Status == RentalStatus.Active || r.Status == RentalStatus.Extended)
                                     && r.Period.EndDate < today)
                            .ToList();

                        // Get unique tenant IDs
                        tenantIds = allRentals.Select(r => r.TenantId).Distinct().ToList();
                    }

                    _logger.LogInformation("[Hangfire] Found {RentalCount} expired rentals across {TenantCount} tenant(s) to update",
                        allRentals.Count, tenantIds.Count);

                    // Process each tenant separately
                    foreach (var tenantId in tenantIds)
                    {
                        using (_currentTenant.Change(tenantId))
                        {
                            var tenantRentals = allRentals.Where(r => r.TenantId == tenantId).ToList();

                            _logger.LogInformation("[Hangfire] Processing {RentalCount} expired rentals for tenant {TenantId}",
                                tenantRentals.Count, tenantId);

                            // Process each rental
                            foreach (var rental in tenantRentals)
                            {
                                try
                                {
                                    var oldStatus = rental.Status;
                                    rental.AutoExpire();

                                    await _rentalRepository.UpdateAsync(rental);
                                    rentalsExpired++;

                                    _logger.LogInformation(
                                        "[Hangfire] Rental {RentalId} for Booth {BoothId} expired: {OldStatus} -> Expired (End date: {EndDate})",
                                        rental.Id, rental.BoothId, oldStatus, rental.Period.EndDate);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex,
                                        "[Hangfire] Failed to expire rental {RentalId} for tenant {TenantId}",
                                        rental.Id, tenantId);
                                }
                            }
                        }
                    }

                    await uow.CompleteAsync();

                    _logger.LogInformation(
                        "[Hangfire] Daily rental status sync completed. Total expired: {ExpiredCount}",
                        rentalsExpired);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Hangfire] Error during daily rental status synchronization");
                    throw;
                }
            }
        }
    }
}
