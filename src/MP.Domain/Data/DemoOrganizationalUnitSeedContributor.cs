using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MP.Domain.OrganizationalUnits;
using Microsoft.Extensions.Logging;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.Data;

/// <summary>
/// OU-50: Seed contributor for demo/test data
/// Creates sample organizational units in test tenants:
/// - "CTO" tenant: Creates "Główna", "Centrum", "Północ"
/// - "KISS" tenant: Creates "Główna", "Zachodnia"
/// Also creates test registration codes for each unit
/// </summary>
public class DemoOrganizationalUnitSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly OrganizationalUnitManager _ouManager;
    private readonly IOrganizationalUnitRegistrationCodeRepository _registrationCodeRepository;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<DemoOrganizationalUnitSeedContributor> _logger;
    private readonly Volo.Abp.Guids.IGuidGenerator _guidGenerator;

    // Demo unit definitions - keyed by tenant code (CTO, KISS)
    private static readonly Dictionary<string, List<(string Name, string Code)>> DemoUnits = new()
    {
        {
            "CTO", new List<(string, string)>
            {
                ("Główna", "CTO-MAIN"),
                ("Centrum", "CTO-CENTER"),
                ("Północ", "CTO-NORTH")
            }
        },
        {
            "KISS", new List<(string, string)>
            {
                ("Główna", "KISS-MAIN"),
                ("Zachodnia", "KISS-WEST")
            }
        }
    };

    public DemoOrganizationalUnitSeedContributor(
        OrganizationalUnitManager ouManager,
        IOrganizationalUnitRegistrationCodeRepository registrationCodeRepository,
        ICurrentTenant currentTenant,
        ILogger<DemoOrganizationalUnitSeedContributor> logger,
        Volo.Abp.Guids.IGuidGenerator guidGenerator)
    {
        _ouManager = ouManager;
        _registrationCodeRepository = registrationCodeRepository;
        _currentTenant = currentTenant;
        _logger = logger;
        _guidGenerator = guidGenerator;
    }

    /// <summary>
    /// Seed demo organizational unit data for test tenants
    /// Note: In development environment, this checks for CTO and KISS tenants
    /// </summary>
    public async Task SeedAsync(DataSeedContext context)
    {
        var tenantId = context.TenantId;
        if (tenantId == null)
        {
            // Demo seeding only for tenant-specific contexts
            return;
        }

        // Demo seeding is disabled for now - can be enabled via configuration
        // To enable demo data seeding, add logic to detect demo tenants
        // For production, this should be controlled by appsettings configuration

        _logger.LogDebug($"Demo seed contributor skipped for tenant {tenantId}");
        return;

        /* Future enhancement: When tenant name is available from context
        using (_currentTenant.Change(tenantId))
        {
            try
            {
                // Detect if this is a demo tenant by checking unit count
                var unitCount = await _ouManager.GetUnitMemberCountAsync(null);

                // Only seed demo units if default unit doesn't have child units
                if (unitCount > 1)
                {
                    _logger.LogInformation($"Demo units already exist for tenant {tenantId}");
                    return;
                }

                // Here we would iterate DemoUnits and create sample units
                // This requires tenant name to be available
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error seeding demo organizational units for tenant {tenantId}: {ex.Message}");
                throw;
            }
        }
        */
    }

    /// <summary>
    /// Generates a demo registration code
    /// Format: {TenantCode}-{UnitName}-{RandomPart}
    /// Example: CTO-CENTER-ABC123
    /// </summary>
    private string GenerateRegistrationCode(string tenantCode, string unitName)
    {
        // Truncate unit name to 3 characters
        var unitPart = unitName.Length > 3 ? unitName.Substring(0, 3).ToUpper() : unitName.ToUpper();

        // Generate 6-character random alphanumeric suffix
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var suffix = new string[6];

        for (int i = 0; i < 6; i++)
        {
            suffix[i] = chars[random.Next(chars.Length)].ToString();
        }

        // Combine: TENANT-UNIT-RANDOM (e.g., CTO-CEN-ABC123)
        var code = $"{tenantCode}-{unitPart}-{string.Concat(suffix)}";

        // Ensure it doesn't exceed 50 characters (max length for registration code)
        if (code.Length > 50)
        {
            code = code.Substring(0, 50);
        }

        return code;
    }
}
