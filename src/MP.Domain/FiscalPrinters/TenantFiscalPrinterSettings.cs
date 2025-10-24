using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.FiscalPrinters
{
    /// <summary>
    /// Fiscal printer configuration per tenant
    /// Each tenant can have multiple fiscal printers configured
    /// </summary>
    public class TenantFiscalPrinterSettings : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; private set; }
        public Guid OrganizationalUnitId { get; private set; }

        /// <summary>
        /// Fiscal printer provider identifier (e.g., "posnet_thermal", "elzab", "novitus")
        /// </summary>
        public string ProviderId { get; private set; } = null!;

        /// <summary>
        /// Display name for this fiscal printer configuration
        /// </summary>
        public string DisplayName { get; private set; } = null!;

        /// <summary>
        /// Whether this fiscal printer configuration is enabled
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// Whether this is the active (default) fiscal printer for checkout
        /// Only one fiscal printer per tenant can be active at a time
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Configuration JSON (provider-specific settings like port name, baud rate, tax ID, etc.)
        /// </summary>
        public string ConfigurationJson { get; private set; } = "{}";

        /// <summary>
        /// Region/country code
        /// </summary>
        public string Region { get; private set; } = "PL";

        /// <summary>
        /// Company name for fiscal receipts
        /// </summary>
        public string? CompanyName { get; private set; }

        /// <summary>
        /// Tax identification number (NIP in Poland)
        /// </summary>
        public string? TaxId { get; private set; }

        private TenantFiscalPrinterSettings() { }

        public TenantFiscalPrinterSettings(
            Guid id,
            Guid organizationalUnitId,
            Guid? tenantId,
            string providerId,
            string displayName,
            string configurationJson,
            string region = "PL",
            bool isEnabled = true,
            bool isActive = false) : base(id)
        {
            TenantId = tenantId;
            OrganizationalUnitId = organizationalUnitId;
            SetProviderId(providerId);
            SetDisplayName(displayName);
            ConfigurationJson = configurationJson ?? "{}";
            Region = region;
            IsEnabled = isEnabled;
            IsActive = isActive;
        }

        public void SetProviderId(string providerId)
        {
            if (string.IsNullOrWhiteSpace(providerId))
                throw new ArgumentException("Provider ID cannot be empty", nameof(providerId));

            ProviderId = providerId.Trim().ToLowerInvariant();
        }

        public void SetDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name cannot be empty", nameof(displayName));

            DisplayName = displayName.Trim();
        }

        public void SetConfiguration(string configurationJson)
        {
            ConfigurationJson = configurationJson ?? "{}";
        }

        public void Enable()
        {
            IsEnabled = true;
        }

        public void Disable()
        {
            IsEnabled = false;
        }

        public void SetRegion(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
                throw new ArgumentException("Region cannot be empty", nameof(region));

            Region = region.Trim().ToUpperInvariant();
        }

        public void SetCompanyInfo(string? companyName, string? taxId)
        {
            CompanyName = companyName?.Trim();
            TaxId = taxId?.Trim();
        }

        public void SetAsActive()
        {
            IsActive = true;
        }

        public void SetAsInactive()
        {
            IsActive = false;
        }
    }
}
