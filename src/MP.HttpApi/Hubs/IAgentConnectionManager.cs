using System;
using System.Threading.Tasks;
using MP.LocalAgent.Contracts.Models;

namespace MP.HttpApi.Hubs
{
    /// <summary>
    /// Interface for managing agent connections
    /// </summary>
    public interface IAgentConnectionManager
    {
        /// <summary>
        /// Register an agent connection
        /// </summary>
        Task RegisterAgentAsync(Guid tenantId, string agentId, string connectionId, string? userId);

        /// <summary>
        /// Unregister an agent connection
        /// </summary>
        Task UnregisterAgentAsync(Guid tenantId, string agentId, string connectionId);

        /// <summary>
        /// Update agent information
        /// </summary>
        Task UpdateAgentInfoAsync(Guid tenantId, string agentId, string connectionId, AgentDeviceInfo deviceInfo);

        /// <summary>
        /// Update agent heartbeat
        /// </summary>
        Task UpdateHeartbeatAsync(Guid tenantId, string agentId, string connectionId);

        /// <summary>
        /// Update device status
        /// </summary>
        Task UpdateDeviceStatusAsync(Guid tenantId, string agentId, string deviceId, MP.LocalAgent.Contracts.Enums.DeviceStatus status, string? details);

        /// <summary>
        /// Get active agent for tenant
        /// </summary>
        Task<AgentConnectionInfo?> GetActiveAgentAsync(Guid tenantId);

        /// <summary>
        /// Get all agents for tenant
        /// </summary>
        Task<System.Collections.Generic.List<AgentConnectionInfo>> GetAgentsAsync(Guid tenantId);

        /// <summary>
        /// Check if agent is connected
        /// </summary>
        Task<bool> IsAgentConnectedAsync(Guid tenantId, string agentId);

        /// <summary>
        /// Get device status for agent
        /// </summary>
        Task<DeviceStatusInfo?> GetDeviceStatusAsync(Guid tenantId, string agentId, string deviceId);

        /// <summary>
        /// Get connection statistics
        /// </summary>
        Task<ConnectionStatistics> GetStatisticsAsync();
    }

    /// <summary>
    /// Agent connection information
    /// </summary>
    public class AgentConnectionInfo
    {
        public Guid TenantId { get; set; }
        public string AgentId { get; set; } = null!;
        public string ConnectionId { get; set; } = null!;
        public string? UserId { get; set; }
        public DateTime ConnectedAt { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public AgentDeviceInfo DeviceInfo { get; set; } = null!;
        public bool IsActive => DateTime.UtcNow - LastHeartbeat < TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Device status information
    /// </summary>
    public class DeviceStatusInfo
    {
        public string DeviceId { get; set; } = null!;
        public MP.LocalAgent.Contracts.Enums.DeviceStatus Status { get; set; }
        public string? Details { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Connection statistics
    /// </summary>
    public class ConnectionStatistics
    {
        public int TotalAgents { get; set; }
        public int ActiveAgents { get; set; }
        public int TotalDevices { get; set; }
        public int OnlineDevices { get; set; }
        public System.Collections.Generic.Dictionary<string, int> DevicesByType { get; set; } = new();
        public System.Collections.Generic.Dictionary<string, int> AgentsByTenant { get; set; } = new();
    }

    /// <summary>
    /// Agent registration request
    /// </summary>
    public class AgentRegistrationRequest
    {
        public AgentDeviceInfo DeviceInfo { get; set; } = null!;
    }

    /// <summary>
    /// Device status report request
    /// </summary>
    public class DeviceStatusReportRequest
    {
        public string DeviceId { get; set; } = null!;
        public MP.LocalAgent.Contracts.Enums.DeviceStatus Status { get; set; }
        public string? Details { get; set; }
    }

    /// <summary>
    /// Terminal command request
    /// </summary>
    public class TerminalCommandRequest
    {
        public string CommandType { get; set; } = null!;
        public object CommandData { get; set; } = null!;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);
    }

    /// <summary>
    /// Fiscal printer command request
    /// </summary>
    public class FiscalPrinterCommandRequest
    {
        public string CommandType { get; set; } = null!;
        public object CommandData { get; set; } = null!;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
    }
}