using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MP.LocalAgent.Contracts.Models;

namespace MP.LocalAgent.Interfaces
{
    /// <summary>
    /// Manages command queue for processing device operations
    /// </summary>
    public interface ICommandQueue
    {
        /// <summary>
        /// Enqueue a command for processing
        /// </summary>
        Task EnqueueCommandAsync<T>(T command) where T : class;

        /// <summary>
        /// Dequeue the next command for processing
        /// </summary>
        Task<CommandInfo?> DequeueCommandAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a specific command by ID
        /// </summary>
        Task<CommandInfo?> GetCommandAsync(Guid commandId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update command status
        /// </summary>
        Task UpdateCommandStatusAsync(Guid commandId, Enums.CommandStatus status, object? response = null);

        /// <summary>
        /// Get all pending commands
        /// </summary>
        Task<List<CommandInfo>> GetPendingCommandsAsync();

        /// <summary>
        /// Cancel a command
        /// </summary>
        Task CancelCommandAsync(Guid commandId);

        /// <summary>
        /// Get commands by status
        /// </summary>
        Task<List<CommandInfo>> GetCommandsByStatusAsync(Enums.CommandStatus status);

        /// <summary>
        /// Get commands for a specific device
        /// </summary>
        Task<List<CommandInfo>> GetCommandsForDeviceAsync(string deviceId);

        /// <summary>
        /// Get queue statistics
        /// </summary>
        Task<QueueStatistics> GetStatisticsAsync();

        /// <summary>
        /// Clear completed commands older than specified time
        /// </summary>
        Task<int> ClearCompletedCommandsAsync(TimeSpan olderThan);

        /// <summary>
        /// Event fired when a command is enqueued
        /// </summary>
        event EventHandler<CommandEnqueuedEventArgs>? CommandEnqueued;

        /// <summary>
        /// Event fired when a command status is updated
        /// </summary>
        event EventHandler<CommandStatusUpdatedEventArgs>? CommandStatusUpdated;
    }

    /// <summary>
    /// Queue statistics
    /// </summary>
    public class QueueStatistics
    {
        public int TotalCommands { get; set; }
        public int PendingCommands { get; set; }
        public int ProcessingCommands { get; set; }
        public int CompletedCommands { get; set; }
        public int FailedCommands { get; set; }
        public int TimedOutCommands { get; set; }
        public int CancelledCommands { get; set; }
        public DateTime OldestPendingCommand { get; set; }
        public TimeSpan AverageProcessingTime { get; set; }
    }

    /// <summary>
    /// Command enqueued event arguments
    /// </summary>
    public class CommandEnqueuedEventArgs : EventArgs
    {
        public CommandInfo Command { get; set; } = null!;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Command status updated event arguments
    /// </summary>
    public class CommandStatusUpdatedEventArgs : EventArgs
    {
        public CommandInfo Command { get; set; } = null!;
        public Enums.CommandStatus PreviousStatus { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}