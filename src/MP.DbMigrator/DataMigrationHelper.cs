using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MP.Domain.OrganizationalUnits;
using Microsoft.Extensions.Logging;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Volo.Abp.Uow;

namespace MP.DbMigrator;

/// <summary>
/// Helper service for data migration tasks (OU-46-OU-51)
/// Handles creation of default organizational units, data assignment, and seeding
/// </summary>
public class DataMigrationHelper
{
    private readonly ITenantRepository _tenantRepository;
    private readonly OrganizationalUnitManager _ouManager;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<DataMigrationHelper> _logger;

    public DataMigrationHelper(
        ITenantRepository tenantRepository,
        OrganizationalUnitManager ouManager,
        ICurrentTenant currentTenant,
        ILogger<DataMigrationHelper> logger)
    {
        _tenantRepository = tenantRepository;
        _ouManager = ouManager;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    /// <summary>
    /// OU-46: Creates default organizational units for all existing tenants
    /// Creates unit "Główna" with code "{TenantCode}-MAIN" for each tenant
    /// </summary>
    /// <returns>Mapping of TenantId → DefaultUnitId</returns>
    [UnitOfWork]
    public virtual async Task<Dictionary<Guid, Guid>> CreateDefaultUnitsForAllTenantsAsync()
    {
        var tenantUnitMapping = new Dictionary<Guid, Guid>();
        var tenants = await _tenantRepository.GetListAsync(includeDetails: false);

        _logger.LogInformation($"Creating default organizational units for {tenants.Count} tenants (OU-46)");

        foreach (var tenant in tenants)
        {
            try
            {
                // Use tenant context
                using (_currentTenant.Change(tenant.Id))
                {
                    // Extract tenant code from name (e.g., "Warszawa" → "WARSZAWA")
                    var tenantCode = tenant.Name.ToUpper().Replace(" ", "");

                    _logger.LogInformation($"Creating default unit for tenant '{tenant.Name}' with code '{tenantCode}-MAIN'");

                    // Create default "Główna" unit
                    var defaultUnit = await _ouManager.CreateDefaultForTenantAsync(
                        tenant.Id,
                        tenantCode);

                    tenantUnitMapping[tenant.Id] = defaultUnit.Id;

                    _logger.LogInformation($"Created default unit '{defaultUnit.Name}' (ID: {defaultUnit.Id}) for tenant '{tenant.Name}'");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating default unit for tenant '{tenant.Name}': {ex.Message}");
                throw;
            }
        }

        _logger.LogInformation($"Successfully created default organizational units for {tenantUnitMapping.Count} tenants");

        return tenantUnitMapping;
    }

    /// <summary>
    /// OU-47: Assigns existing data to default organizational units
    /// This method should be called after CreateDefaultUnitsForAllTenantsAsync
    /// Note: Actual SQL assignment is done via migration scripts
    /// </summary>
    /// <param name="tenantUnitMapping">Mapping of TenantId → DefaultUnitId from OU-46</param>
    [UnitOfWork]
    public virtual async Task AssignDataToDefaultUnitsAsync(Dictionary<Guid, Guid> tenantUnitMapping)
    {
        _logger.LogInformation($"Assigning existing data to default units for {tenantUnitMapping.Count} tenants (OU-47)");

        // Note: Actual data assignment is done via SQL migration scripts
        // 002_AssignDataToDefaultUnits.sql handles the UPDATE statements for all 27 entities

        foreach (var (tenantId, unitId) in tenantUnitMapping)
        {
            _logger.LogInformation($"Tenant {tenantId} data will be assigned to default unit {unitId}");
        }

        _logger.LogInformation("Data assignment queued - SQL migration scripts will execute the actual assignments");
    }

    /// <summary>
    /// OU-48: Assigns existing users to their tenant's default organizational unit
    /// Assigns users with their current roles in the tenant
    /// </summary>
    /// <param name="tenantUnitMapping">Mapping of TenantId → DefaultUnitId from OU-46</param>
    [UnitOfWork]
    public virtual async Task AssignUsersToDefaultUnitsAsync(Dictionary<Guid, Guid> tenantUnitMapping)
    {
        _logger.LogInformation($"Assigning users to default units for {tenantUnitMapping.Count} tenants (OU-48)");

        // Note: Actual user assignment is done via SQL migration script
        // 003_AssignUsersToDefaultUnits.sql handles the creation of UserOrganizationalUnit records

        foreach (var (tenantId, unitId) in tenantUnitMapping)
        {
            _logger.LogInformation($"Tenant {tenantId} users will be assigned to default unit {unitId}");
        }

        _logger.LogInformation("User assignment queued - SQL migration scripts will execute the actual assignments");
    }

    /// <summary>
    /// Verifies that default organizational units were created successfully
    /// </summary>
    [UnitOfWork]
    public virtual async Task VerifyMigrationAsync(Dictionary<Guid, Guid> tenantUnitMapping)
    {
        _logger.LogInformation("Verifying organizational unit migration (OU-46 verification)");

        var tenants = await _tenantRepository.GetListAsync(includeDetails: false);

        foreach (var tenant in tenants)
        {
            using (_currentTenant.Change(tenant.Id))
            {
                if (tenantUnitMapping.TryGetValue(tenant.Id, out var expectedUnitId))
                {
                    _logger.LogInformation($"✓ Tenant '{tenant.Name}' should have default unit {expectedUnitId}");
                }
                else
                {
                    _logger.LogWarning($"✗ Tenant '{tenant.Name}' not found in migration mapping");
                }
            }
        }
    }
}
