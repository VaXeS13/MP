using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.Terminals
{
    /// <summary>
    /// Payment terminal configuration per tenant
    /// Each tenant can have multiple terminal providers configured
    /// </summary>
    public class TenantTerminalSettings : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; private set; }
        public Guid OrganizationalUnitId { get; private set; }

        /// <summary>
        /// Terminal provider identifier (e.g., "ingenico", "verifone", "stripe_terminal", "sumup", "adyen", "mock")
        /// </summary>
        public string ProviderId { get; private set; } = null!;

        /// <summary>
        /// Display name for this terminal configuration
        /// </summary>
        public string DisplayName { get; private set; } = null!;

        /// <summary>
        /// Whether this terminal configuration is enabled
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// Whether this is the active (default) terminal for checkout
        /// Only one terminal per tenant can be active at a time
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Configuration JSON (provider-specific settings like API keys, merchant IDs, etc.)
        /// </summary>
        public string ConfigurationJson { get; private set; } = "{}";

        /// <summary>
        /// Currency code for terminal transactions
        /// </summary>
        public string Currency { get; private set; } = "PLN";

        /// <summary>
        /// Region/country code
        /// </summary>
        public string? Region { get; private set; }

        /// <summary>
        /// Whether this is a sandbox/test configuration
        /// </summary>
        public bool IsSandbox { get; private set; }

        private TenantTerminalSettings() { }

        public TenantTerminalSettings(
            Guid id,
            Guid organizationalUnitId,
            Guid? tenantId,
            string providerId,
            string displayName,
            string configurationJson,
            string currency = "PLN",
            bool isEnabled = true,
            bool isActive = false,
            bool isSandbox = false) : base(id)
        {
            TenantId = tenantId;
            OrganizationalUnitId = organizationalUnitId;
            SetProviderId(providerId);
            SetDisplayName(displayName);
            ConfigurationJson = configurationJson ?? "{}";
            Currency = currency;
            IsEnabled = isEnabled;
            IsActive = isActive;
            IsSandbox = isSandbox;
        }

        public void SetProviderId(string providerId)
        {
            if (string.IsNullOrWhiteSpace(providerId))
                throw new ArgumentException("Provider ID cannot be empty", nameof(providerId));

            ProviderId = providerId.Trim().ToLowerInvariant();
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

        public void SetCurrency(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency cannot be empty", nameof(currency));

            Currency = currency.Trim().ToUpperInvariant();
        }

        public void SetRegion(string? region)
        {
            Region = region?.Trim();
        }

        public void SetSandboxMode(bool isSandbox)
        {
            IsSandbox = isSandbox;
        }

        public void SetDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name cannot be empty", nameof(displayName));

            DisplayName = displayName.Trim();
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