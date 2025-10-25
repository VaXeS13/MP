using System;
using System.Threading.Tasks;
using MP.Domain.OrganizationalUnits;
using Microsoft.Extensions.Logging;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.Data;

/// <summary>
/// OU-49: Seed contributor for new tenants
/// Automatically creates default organizational unit "Główna" when a new tenant is created
/// Also generates initial registration code for the unit
/// </summary>
public class NewTenantOrganizationalUnitSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly OrganizationalUnitManager _ouManager;
    private readonly IOrganizationalUnitRegistrationCodeRepository _registrationCodeRepository;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<NewTenantOrganizationalUnitSeedContributor> _logger;
    private readonly Volo.Abp.Guids.IGuidGenerator _guidGenerator;

    public NewTenantOrganizationalUnitSeedContributor(
        OrganizationalUnitManager ouManager,
        IOrganizationalUnitRegistrationCodeRepository registrationCodeRepository,
        ICurrentTenant currentTenant,
        ILogger<NewTenantOrganizationalUnitSeedContributor> logger,
        Volo.Abp.Guids.IGuidGenerator guidGenerator)
    {
        _ouManager = ouManager;
        _registrationCodeRepository = registrationCodeRepository;
        _currentTenant = currentTenant;
        _logger = logger;
        _guidGenerator = guidGenerator;
    }

    /// <summary>
    /// Seed data for new tenants - creates default organizational unit
    /// </summary>
    public async Task SeedAsync(DataSeedContext context)
    {
        var tenantId = context.TenantId;
        if (tenantId == null)
        {
            // This seeder only works with tenant context (not host)
            return;
        }

        using (_currentTenant.Change(tenantId))
        {
            try
            {
                // Generate tenant code for organizational unit
                // For new tenants, use a simple code pattern based on tenant ID
                // In production, this should be called from tenant creation handler which has tenant name
                var tenantCode = $"TENANT-{tenantId:N}".Substring(0, 50).TrimEnd('-');

                _logger.LogInformation($"Creating default organizational unit for tenant {tenantId} (OU-49)");

                // Create default "Główna" unit for the tenant
                var defaultUnit = await _ouManager.CreateDefaultForTenantAsync(
                    tenantId,
                    tenantCode);

                _logger.LogInformation($"✓ Created default unit '{defaultUnit.Name}' (ID: {defaultUnit.Id}) for tenant {tenantId}");

                // Generate initial registration code for the unit
                // No expiry, unlimited usage - can be used by anyone to join this unit
                var registrationCode = new OrganizationalUnitRegistrationCode(
                    id: _guidGenerator.Create(),
                    organizationalUnitId: defaultUnit.Id,
                    tenantId: tenantId,
                    code: GenerateRegistrationCode(),
                    roleId: null,  // No specific role assignment
                    expiresAt: null,  // No expiration
                    maxUsageCount: null);  // Unlimited usage

                await _registrationCodeRepository.InsertAsync(registrationCode);

                _logger.LogInformation($"✓ Generated registration code '{registrationCode.Code}' for default unit");
                _logger.LogInformation($"✓ Seed completed for new tenant {tenantId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error seeding organizational units for tenant {tenantId}: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// Generates a random registration code (8 characters, alphanumeric)
    /// Format: e.g., "ABC12345"
    /// </summary>
    private string GenerateRegistrationCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var code = new string[8];

        for (int i = 0; i < 8; i++)
        {
            code[i] = chars[random.Next(chars.Length)].ToString();
        }

        return string.Concat(code);
    }
}
