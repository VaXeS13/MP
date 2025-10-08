using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using MP.Domain.Rentals;
using MP.Domain.Booths;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Data;
using MP.Rentals;

namespace MP.Application.Payments
{
    /// <summary>
    /// Hangfire recurring job that synchronizes booth statuses based on rental periods
    /// Runs daily at 00:05 to ensure booth statuses are accurate
    /// </summary>
    public class DailyBoothStatusSyncJob : ITransientDependency
    {
        private readonly IBoothRepository _boothRepository;
        private readonly IRepository<Rental, Guid> _rentalRepository;
        private readonly ILogger<DailyBoothStatusSyncJob> _logger;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly ICurrentTenant _currentTenant;
        private readonly IDataFilter<IMultiTenant> _dataFilter;

        public DailyBoothStatusSyncJob(
            IBoothRepository boothRepository,
            IRepository<Rental, Guid> rentalRepository,
            ILogger<DailyBoothStatusSyncJob> logger,
            IUnitOfWorkManager unitOfWorkManager,
            ICurrentTenant currentTenant,
            IDataFilter<IMultiTenant> dataFilter)
        {
            _boothRepository = boothRepository;
            _rentalRepository = rentalRepository;
            _logger = logger;
            _unitOfWorkManager = unitOfWorkManager;
            _currentTenant = currentTenant;
            _dataFilter = dataFilter;
        }

        [AutomaticRetry(Attempts = 3)]
        public async Task ExecuteAsync()
        {
            _logger.LogInformation("[Hangfire] Starting daily booth status synchronization job");

            using (var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true))
            {
                try
                {
                    int boothsUpdated = 0;
                    int boothsMarkedRented = 0;
                    int boothsMarkedAvailable = 0;
                    int boothsMarkedReserved = 0;

                    List<Booth> allBooths;
                    List<Guid?> tenantIds;

                    // Disable multi-tenant filter to process all tenants
                    using (_dataFilter.Disable())
                    {
                        _logger.LogInformation("[Hangfire] Multi-tenant filter disabled, fetching booths from all tenants");

                        // Get all booths except those in Maintenance (Maintenance is managed manually by admins)
                        allBooths = await _boothRepository.GetListAsync(b => b.Status != BoothStatus.Maintenance);

                        // Get unique tenant IDs
                        tenantIds = allBooths.Select(b => b.TenantId).Distinct().ToList();
                    }

                    _logger.LogInformation("[Hangfire] Found {BoothCount} booths across {TenantCount} tenant(s) to check",
                        allBooths.Count, tenantIds.Count);

                    var today = DateTime.Today;

                    // Process each tenant separately
                    foreach (var tenantId in tenantIds)
                    {
                        using (_currentTenant.Change(tenantId))
                        {
                            var tenantBooths = allBooths.Where(b => b.TenantId == tenantId).ToList();

                            _logger.LogInformation("[Hangfire] Processing {BoothCount} booths for tenant {TenantId}",
                                tenantBooths.Count, tenantId);

                            // Get all rentals for this tenant that could affect booth status
                            var relevantRentals = await _rentalRepository.GetListAsync(r =>
                                r.Payment.PaymentStatus == PaymentStatus.Completed &&
                                (r.Status == RentalStatus.Active || r.Status == RentalStatus.Extended) &&
                                r.Period.EndDate >= today);

                            _logger.LogInformation("[Hangfire] Found {RentalCount} active paid rentals for tenant {TenantId}",
                                relevantRentals.Count, tenantId);

                            // Create lookup dictionary for faster access: BoothId -> Active Rental
                            var boothRentalMap = relevantRentals
                                .Where(r => r.Period.StartDate <= today && r.Period.EndDate >= today)
                                .GroupBy(r => r.BoothId)
                                .ToDictionary(g => g.Key, g => g.First());

                            // Create lookup for future rentals (paid but not started yet)
                            var boothFutureRentalMap = relevantRentals
                                .Where(r => r.Period.StartDate > today)
                                .GroupBy(r => r.BoothId)
                                .ToDictionary(g => g.Key, g => g.First());

                            // Process each booth
                            foreach (var booth in tenantBooths)
                            {
                                var expectedStatus = DetermineBoothStatus(booth, boothRentalMap, boothFutureRentalMap, today);

                                if (booth.Status != expectedStatus)
                                {
                                    var oldStatus = booth.Status;

                                    // Update booth status
                                    switch (expectedStatus)
                                    {
                                        case BoothStatus.Rented:
                                            booth.MarkAsRented();
                                            boothsMarkedRented++;
                                            break;
                                        case BoothStatus.Reserved:
                                            booth.MarkAsReserved();
                                            boothsMarkedReserved++;
                                            break;
                                        case BoothStatus.Available:
                                            booth.MarkAsAvailable();
                                            boothsMarkedAvailable++;
                                            break;
                                    }

                                    await _boothRepository.UpdateAsync(booth);
                                    boothsUpdated++;

                                    _logger.LogInformation("[Hangfire] Booth {BoothId} ({BoothNumber}) status changed: {OldStatus} -> {NewStatus}",
                                        booth.Id, booth.Number, oldStatus, expectedStatus);
                                }
                            }
                        }
                    }

                    await uow.CompleteAsync();

                    _logger.LogInformation(
                        "[Hangfire] Daily booth status sync completed. Total updated: {UpdatedCount} " +
                        "(Rented: {RentedCount}, Available: {AvailableCount}, Reserved: {ReservedCount})",
                        boothsUpdated, boothsMarkedRented, boothsMarkedAvailable, boothsMarkedReserved);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Hangfire] Error during daily booth status synchronization");
                    throw;
                }
            }
        }

        private BoothStatus DetermineBoothStatus(
            Booth booth,
            Dictionary<Guid, Rental> activeRentalMap,
            Dictionary<Guid, Rental> futureRentalMap,
            DateTime today)
        {
            // Priority 1: Maintenance status is never changed by this job
            if (booth.Status == BoothStatus.Maintenance)
            {
                return BoothStatus.Maintenance;
            }

            // Priority 2: Check if there's an active rental for TODAY
            if (activeRentalMap.ContainsKey(booth.Id))
            {
                return BoothStatus.Rented;
            }

            // Priority 3: Check if there's a paid future rental (Reserved status)
            if (futureRentalMap.ContainsKey(booth.Id))
            {
                return BoothStatus.Reserved;
            }

            // Default: No active or future rental = Available
            return BoothStatus.Available;
        }
    }
}
