using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.LocalAgent
{
    /// <summary>
    /// API Key for Local Agent authentication
    /// Supports multi-tenant agent authentication with SHA256 hashing
    /// </summary>
    public class AgentApiKey : FullAuditedEntity<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }
        public Guid OrganizationalUnitId { get; set; }

        /// <summary>
        /// Agent identifier (e.g., "shop-warsaw-001")
        /// </summary>
        [Required]
        [StringLength(100)]
        public string AgentId { get; set; } = null!;

        /// <summary>
        /// API Key prefix (e.g., "mp_agent_")
        /// Used for display and identification
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Prefix { get; set; } = "mp_agent_";

        /// <summary>
        /// Last 8 characters of API key for identification
        /// Format: mp_agent_...suffix (e.g., mp_agent_...z1t5v7x9)
        /// </summary>
        [Required]
        [StringLength(8)]
        public string Suffix { get; set; } = null!;

        /// <summary>
        /// SHA256 hash of the API key
        /// The actual API key is NEVER stored in plaintext
        /// </summary>
        [Required]
        [StringLength(64)]  // SHA256 produces 64 hex characters
        public string KeyHash { get; set; } = null!;

        /// <summary>
        /// Salt for SHA256 hashing (16 bytes as hex = 32 characters)
        /// </summary>
        [Required]
        [StringLength(32)]
        public string Salt { get; set; } = null!;

        /// <summary>
        /// API key name for admin identification
        /// Example: "Production Key", "Development Key", "Backup Terminal"
        /// </summary>
        [StringLength(255)]
        public string? Name { get; set; }

        /// <summary>
        /// Optional description of the key's purpose
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Date when the API key expires (90 days from creation)
        /// </summary>
        [Required]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Date when the API key was last used for authentication
        /// Used for monitoring and auditing
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// Number of times this key has been used
        /// For monitoring and analytics
        /// </summary>
        public long UsageCount { get; set; } = 0;

        /// <summary>
        /// Optional comma-separated list of IP addresses that can use this key
        /// Format: "192.168.1.100,10.0.0.5" or empty to allow all IPs
        /// </summary>
        [StringLength(1000)]
        public string? IpWhitelist { get; set; }

        /// <summary>
        /// Is the API key currently active and usable
        /// </summary>
        [Required]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Rotation flag - when true, indicates this key should be rotated soon
        /// </summary>
        [Required]
        public bool ShouldRotate { get; set; } = false;

        /// <summary>
        /// Date when this key was rotated from a previous key
        /// Used to track key rotation history
        /// </summary>
        public DateTime? RotatedFromKeyId { get; set; }

        /// <summary>
        /// Current number of failed authentication attempts
        /// Resets on successful authentication
        /// </summary>
        public int FailedAuthenticationAttempts { get; set; } = 0;

        /// <summary>
        /// Date when this key was last locked due to failed attempts
        /// </summary>
        public DateTime? LockedUntil { get; set; }

        /// <summary>
        /// Check if the API key is currently locked
        /// </summary>
        public bool IsLocked
        {
            get
            {
                if (LockedUntil == null)
                    return false;

                if (DateTime.UtcNow >= LockedUntil)
                {
                    // Lock has expired
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Check if the API key is expired
        /// </summary>
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        /// <summary>
        /// Check if the API key is usable
        /// Usable if: active, not expired, not locked
        /// </summary>
        public bool IsUsable => IsActive && !IsExpired && !IsLocked;

        /// <summary>
        /// Check if an IP address is allowed to use this key
        /// Returns true if IP whitelist is empty (allow all) or IP is in whitelist
        /// </summary>
        public bool IsIpAllowed(string ipAddress)
        {
            if (string.IsNullOrEmpty(IpWhitelist))
            {
                // Empty whitelist = allow all IPs
                return true;
            }

            var allowedIps = IpWhitelist.Split(',');
            foreach (var allowedIp in allowedIps)
            {
                if (allowedIp.Trim() == ipAddress)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Record a failed authentication attempt
        /// After 5 failed attempts, the key is locked for 15 minutes
        /// </summary>
        public void RecordFailedAttempt()
        {
            FailedAuthenticationAttempts++;

            if (FailedAuthenticationAttempts >= 5)
            {
                // Lock the key for 15 minutes
                LockedUntil = DateTime.UtcNow.AddMinutes(15);
            }
        }

        /// <summary>
        /// Record a successful authentication
        /// Resets failed attempts and updates LastUsedAt
        /// </summary>
        public void RecordSuccessfulAttempt()
        {
            FailedAuthenticationAttempts = 0;
            LockedUntil = null;
            LastUsedAt = DateTime.UtcNow;
            UsageCount++;
        }

        /// <summary>
        /// Mark the key for rotation
        /// Called when the key is approaching expiration (10 days before)
        /// </summary>
        public void MarkForRotation()
        {
            ShouldRotate = true;
        }

        /// <summary>
        /// Get readable display format for the API key
        /// Example: "mp_agent_...z1t5v7x9" (masked for security)
        /// </summary>
        public string GetMaskedKey()
        {
            return $"{Prefix}...{Suffix}";
        }
    }
}
