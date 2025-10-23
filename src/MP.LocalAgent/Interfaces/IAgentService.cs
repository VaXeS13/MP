using System;
using System.Threading;
using System.Threading.Tasks;
using MP.LocalAgent.Contracts.Models;

namespace MP.LocalAgent.Interfaces
{
    /// <summary>
    /// Main service for managing the local agent
    /// </summary>
    public interface IAgentService
    {
        /// <summary>
        /// Initialize the agent with tenant and agent IDs
        /// </summary>
        Task InitializeAsync(Guid tenantId, string agentId);

        /// <summary>
        /// Start the agent services
        /// </summary>
        Task<bool> StartAsync();

        /// <summary>
        /// Stop the agent services
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Get device information for the agent
        /// </summary>
        Task<AgentDeviceInfo> GetDeviceInfoAsync();

        /// <summary>
        /// Check if the agent is healthy
        /// </summary>
        Task<bool> IsHealthyAsync();

        /// <summary>
        /// Register the agent with the cloud API
        /// </summary>
        Task RegisterAgentAsync();

        /// <summary>
        /// Send heartbeat to the cloud API
        /// </summary>
        Task SendHeartbeatAsync();

        /// <summary>
        /// Get agent status
        /// </summary>
        AgentStatus GetStatus();

        /// <summary>
        /// Event fired when agent status changes
        /// </summary>
        event EventHandler<AgentStatusChangedEventArgs>? StatusChanged;
    }

    /// <summary>
    /// Agent status
    /// </summary>
    public enum AgentStatus
    {
        Stopped,
        Starting,
        Running,
        Stopping,
        Error
    }

    /// <summary>
    /// Agent status changed event arguments
    /// </summary>
    public class AgentStatusChangedEventArgs : EventArgs
    {
        public AgentStatus PreviousStatus { get; set; }
        public AgentStatus CurrentStatus { get; set; }
        public string? Message { get; set; }
    }
}