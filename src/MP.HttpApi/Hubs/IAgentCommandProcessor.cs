using System;
using System.Threading.Tasks;
using MP.LocalAgent.Contracts.Responses;

namespace MP.HttpApi.Hubs
{
    /// <summary>
    /// Interface for processing agent commands
    /// </summary>
    public interface IAgentCommandProcessor
    {
        /// <summary>
        /// Queue terminal command for execution
        /// </summary>
        Task QueueTerminalCommandAsync(Guid tenantId, string agentId, string commandType, object commandData, TimeSpan timeout);

        /// <summary>
        /// Queue fiscal printer command for execution
        /// </summary>
        Task QueueFiscalPrinterCommandAsync(Guid tenantId, string agentId, string commandType, object commandData, TimeSpan timeout);

        /// <summary>
        /// Process command response from agent
        /// </summary>
        Task ProcessCommandResponseAsync(Guid tenantId, string agentId, CommandResponseBase response);

        /// <summary>
        /// Cancel command execution
        /// </summary>
        Task CancelCommandAsync(Guid tenantId, string agentId, Guid commandId);

        /// <summary>
        /// Get command status
        /// </summary>
        Task<CommandExecutionStatus?> GetCommandStatusAsync(Guid commandId);

        /// <summary>
        /// Get pending commands for agent
        /// </summary>
        Task<System.Collections.Generic.List<QueuedCommandInfo>> GetPendingCommandsAsync(Guid tenantId, string agentId);

        /// <summary>
        /// Clean up completed commands
        /// </summary>
        Task<int> CleanupCompletedCommandsAsync(TimeSpan olderThan);
    }

    /// <summary>
    /// Command execution status
    /// </summary>
    public class CommandExecutionStatus
    {
        public Guid CommandId { get; set; }
        public Guid TenantId { get; set; }
        public string AgentId { get; set; } = null!;
        public string CommandType { get; set; } = null!;
        public MP.LocalAgent.Contracts.Enums.CommandStatus Status { get; set; }
        public DateTime QueuedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public CommandResponseBase? Response { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public TimeSpan Timeout { get; set; }
    }

    /// <summary>
    /// Queued command information
    /// </summary>
    public class QueuedCommandInfo
    {
        public Guid CommandId { get; set; }
        public string CommandType { get; set; } = null!;
        public MP.LocalAgent.Contracts.Enums.CommandStatus Status { get; set; }
        public DateTime QueuedAt { get; set; }
        public TimeSpan Timeout { get; set; }
        public string? SerializedData { get; set; }
    }
}