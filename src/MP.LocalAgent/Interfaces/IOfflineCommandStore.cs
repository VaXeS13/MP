using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MP.LocalAgent.Contracts.Models;

namespace MP.LocalAgent.Interfaces
{
    /// <summary>
    /// Interface for persisting commands to offline storage (SQLite)
    /// </summary>
    public interface IOfflineCommandStore
    {
        /// <summary>
        /// Save a command to persistent storage
        /// </summary>
        Task<bool> SaveCommandAsync(CommandInfo command);

        /// <summary>
        /// Load all pending commands from storage
        /// </summary>
        Task<List<CommandInfo>> LoadPendingCommandsAsync();

        /// <summary>
        /// Update command status in storage
        /// </summary>
        Task<bool> UpdateCommandStatusAsync(Guid commandId, string status);

        /// <summary>
        /// Delete a command from storage
        /// </summary>
        Task<bool> DeleteCommandAsync(Guid commandId);

        /// <summary>
        /// Get command from storage by ID
        /// </summary>
        Task<CommandInfo?> GetCommandAsync(Guid commandId);

        /// <summary>
        /// Clean up old completed commands (older than specified days)
        /// </summary>
        Task<int> CleanupOldCommandsAsync(int olderThanDays);

        /// <summary>
        /// Get total queue size
        /// </summary>
        Task<int> GetQueueSizeAsync();

        /// <summary>
        /// Initialize the storage (create tables if needed)
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Dispose resources
        /// </summary>
        void Dispose();
    }
}
