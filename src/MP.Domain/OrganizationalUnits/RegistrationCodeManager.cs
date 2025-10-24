using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Guids;

namespace MP.Domain.OrganizationalUnits
{
    public class RegistrationCodeManager : DomainService
    {
        private readonly IOrganizationalUnitRegistrationCodeRepository _codeRepository;
        private readonly IOrganizationalUnitRepository _unitRepository;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ICurrentTenant _currentTenant;

        private const string ValidCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int DefaultRandomLength = 6;

        public RegistrationCodeManager(
            IOrganizationalUnitRegistrationCodeRepository codeRepository,
            IOrganizationalUnitRepository unitRepository,
            IGuidGenerator guidGenerator,
            ICurrentTenant currentTenant)
        {
            _codeRepository = codeRepository;
            _unitRepository = unitRepository;
            _guidGenerator = guidGenerator;
            _currentTenant = currentTenant;
        }

        /// <summary>
        /// Generates a human-readable registration code for the specified organizational unit
        /// Format: {TenantCode}-{UnitCode}-{Random}
        /// Example: CTO-MAIN-ABC123
        /// </summary>
        public async Task<OrganizationalUnitRegistrationCode> GenerateCodeAsync(
            Guid unitId,
            Guid? roleId = null,
            int? maxUsageCount = null,
            int? expirationDays = null)
        {
            // Validate unit exists
            var unit = await _unitRepository.GetAsync(unitId);
            if (unit == null)
                throw new BusinessException("RegistrationCode.UnitNotFound", "Organizational unit not found");

            // Get current tenant ID (can be null for host)
            var tenantId = _currentTenant.Id;

            // Get tenant code - for host tenant use "HOST", for others try to get from unit's tenant context
            var tenantCode = GetTenantCodeForUnit(unit);

            // Generate the code
            var randomPart = GenerateRandomPart(DefaultRandomLength);
            var code = GenerateCodeFormat(tenantCode, unit.Code, DefaultRandomLength);

            // Validate uniqueness
            var existingCode = await _codeRepository.FindByCodeAsync(tenantId, code);
            if (existingCode != null)
            {
                // If collision, retry with a new random part
                code = GenerateCodeFormat(tenantCode, unit.Code, DefaultRandomLength);
                existingCode = await _codeRepository.FindByCodeAsync(tenantId, code);
                if (existingCode != null)
                    throw new BusinessException("RegistrationCode.CouldNotGenerateUnique",
                        "Could not generate unique registration code");
            }

            // Create the registration code entity
            var registrationCode = new OrganizationalUnitRegistrationCode(
                id: _guidGenerator.Create(),
                organizationalUnitId: unitId,
                code: code,
                tenantId: tenantId
            );

            // Set optional properties
            if (roleId.HasValue)
                registrationCode.SetRoleId(roleId.Value);

            if (maxUsageCount.HasValue)
                registrationCode.SetMaxUsageCount(maxUsageCount.Value);

            if (expirationDays.HasValue)
                registrationCode.SetExpiresAt(DateTime.UtcNow.AddDays(expirationDays.Value));

            return registrationCode;
        }

        /// <summary>
        /// Validates if a registration code exists, is active, not expired, and hasn't exceeded usage limits
        /// </summary>
        public async Task<OrganizationalUnitRegistrationCode> ValidateCodeAsync(Guid tenantId, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new BusinessException("RegistrationCode.CodeRequired", "Code is required");

            // Try to find the code
            var registrationCode = await _codeRepository.FindByCodeAsync(tenantId, code);
            if (registrationCode == null)
                throw new BusinessException("RegistrationCode.NotFound", "Registration code not found");

            // Check if code is active
            if (!registrationCode.IsActive)
                throw new BusinessException("RegistrationCode.Inactive",
                    "Registration code is inactive");

            // Check if code is expired
            if (registrationCode.IsExpired())
                throw new BusinessException("RegistrationCode.Expired",
                    "Registration code has expired");

            // Check if usage limit is reached
            if (registrationCode.IsUsageLimitReached())
                throw new BusinessException("RegistrationCode.UsageLimitReached",
                    "Registration code usage limit has been reached");

            // Verify code can be used
            if (!registrationCode.CanBeUsed())
                throw new BusinessException("RegistrationCode.CannotBeUsed",
                    "Registration code cannot be used");

            return registrationCode;
        }

        /// <summary>
        /// Marks a registration code as used by incrementing its usage count and setting last used timestamp
        /// </summary>
        public async Task<OrganizationalUnitRegistrationCode> UseCodeAsync(Guid codeId)
        {
            var registrationCode = await _codeRepository.GetAsync(codeId);
            if (registrationCode == null)
                throw new BusinessException("RegistrationCode.NotFound", "Registration code not found");

            // Increment usage count (also sets LastUsedAt automatically)
            registrationCode.IncrementUsageCount();

            return registrationCode;
        }

        /// <summary>
        /// Generates the full registration code format: {tenantCode}-{unitCode}-{randomPart}
        /// </summary>
        public string GenerateCodeFormat(string tenantCode, string unitCode, int randomLength = DefaultRandomLength)
        {
            if (string.IsNullOrWhiteSpace(tenantCode))
                throw new BusinessException("RegistrationCode.TenantCodeRequired", "Tenant code is required");

            if (string.IsNullOrWhiteSpace(unitCode))
                throw new BusinessException("RegistrationCode.UnitCodeRequired", "Unit code is required");

            if (randomLength <= 0 || randomLength > 10)
                throw new BusinessException("RegistrationCode.InvalidRandomLength",
                    "Random part length must be between 1 and 10");

            // Normalize codes (uppercase, max 50 chars total for safety)
            var tenantCodeNormalized = tenantCode.ToUpperInvariant().Trim();
            var unitCodeNormalized = unitCode.ToUpperInvariant().Trim();

            if (!Regex.IsMatch(tenantCodeNormalized, @"^[A-Z0-9-]+$"))
                throw new BusinessException("RegistrationCode.InvalidTenantCode",
                    "Tenant code must contain only alphanumeric characters and hyphens");

            if (!Regex.IsMatch(unitCodeNormalized, @"^[A-Z0-9-]+$"))
                throw new BusinessException("RegistrationCode.InvalidUnitCode",
                    "Unit code must contain only alphanumeric characters and hyphens");

            var randomPart = GenerateRandomPart(randomLength);
            var fullCode = $"{tenantCodeNormalized}-{unitCodeNormalized}-{randomPart}";

            // Validate total length
            if (fullCode.Length > 50)
                throw new BusinessException("RegistrationCode.CodeTooLong",
                    "Generated code exceeds maximum length of 50 characters");

            return fullCode;
        }

        /// <summary>
        /// Generates a random alphanumeric string of specified length (A-Z, 0-9)
        /// </summary>
        private string GenerateRandomPart(int length)
        {
            if (length <= 0 || length > 10)
                throw new BusinessException("RegistrationCode.InvalidLength",
                    "Random part length must be between 1 and 10");

            var random = new Random();
            var result = new string(
                Enumerable.Range(0, length)
                    .Select(_ => ValidCharacters[random.Next(ValidCharacters.Length)])
                    .ToArray()
            );

            return result;
        }

        /// <summary>
        /// Determines the tenant code from the organizational unit
        /// Uses "HOST" for host tenant, otherwise derives from tenant context or configuration
        /// </summary>
        private string GetTenantCodeForUnit(OrganizationalUnit unit)
        {
            // For now, if tenant is null (host tenant), use "HOST"
            if (unit.TenantId == null)
                return "HOST";

            // For tenant-specific units, try to get tenant code from current tenant context
            // In production, this should be injected or retrieved from Tenant entity
            // For now, use a placeholder that can be overridden
            var tenantId = unit.TenantId.Value;

            // TODO: Inject ITenantRepository to get tenant code by ID
            // For now, use tenant ID hash as fallback
            return GenerateTenantCodeFromId(tenantId);
        }

        /// <summary>
        /// Generates a 3-4 character tenant code from tenant ID
        /// Used as fallback when tenant code is not available
        /// </summary>
        private string GenerateTenantCodeFromId(Guid tenantId)
        {
            // Convert first 3 bytes of GUID to uppercase letters
            var guidBytes = tenantId.ToByteArray();
            var code = new string(
                guidBytes.Take(3)
                    .Select(b => ValidCharacters[b % ValidCharacters.Length])
                    .ToArray()
            );

            return code;
        }
    }
}
