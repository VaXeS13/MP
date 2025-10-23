using System;
using System.Collections.Generic;

namespace MP.LocalAgent.Contracts.Models
{
    /// <summary>
    /// Information about the local agent and its devices
    /// </summary>
    public class AgentDeviceInfo
    {
        public Guid TenantId { get; set; }
        public string AgentId { get; set; } = null!;
        public string ComputerName { get; set; } = null!;
        public string? IpAddress { get; set; }
        public string Version { get; set; } = "1.0.0";
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
        public Enums.AgentConnectionStatus ConnectionStatus { get; set; }

        /// <summary>
        /// List of configured devices
        /// </summary>
        public List<DeviceInfo> Devices { get; set; } = new();
    }

    /// <summary>
    /// Information about a specific device
    /// </summary>
    public class DeviceInfo
    {
        public string DeviceId { get; set; } = null!;
        public string DeviceType { get; set; } = null!; // Terminal, FiscalPrinter
        public string ProviderId { get; set; } = null!; // ingenico, novitus, etc.
        public string Model { get; set; } = null!;
        public string? SerialNumber { get; set; }
        public string ConnectionType { get; set; } = null!; // USB, RS232, TCP
        public string? ConnectionDetails { get; set; } // COM3, 192.168.1.100:9100, etc.
        public Enums.DeviceStatus Status { get; set; }
        public DateTime LastStatusUpdate { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> ProviderData { get; set; } = new();
        public bool IsEnabled { get; set; }
        public bool IsPrimary { get; set; } // Primary device for its type
    }

    /// <summary>
    /// Information about a queued or processed command
    /// </summary>
    public class CommandInfo
    {
        public Guid CommandId { get; set; }
        public Guid TenantId { get; set; }
        public string AgentId { get; set; } = null!;
        public string CommandType { get; set; } = null!;
        public string SerializedCommand { get; set; } = null!;
        public Enums.CommandStatus Status { get; set; }
        public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TimeSpan Timeout { get; set; }
        public string? SerializedResponse { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
        public int RetryCount { get; set; }
        public int MaxRetries { get; set; } = 3;
    }
}