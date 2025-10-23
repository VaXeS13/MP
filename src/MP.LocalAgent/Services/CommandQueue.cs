using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MP.LocalAgent.Contracts.Models;
using MP.LocalAgent.Contracts.Enums;
using MP.LocalAgent.Exceptions;
using MP.LocalAgent.Interfaces;

namespace MP.LocalAgent.Services
{
    /// <summary>
    /// Thread-safe command queue for processing device operations with offline persistence
    /// </summary>
    public class CommandQueue : ICommandQueue
    {
        private readonly ILogger<CommandQueue> _logger;
        private readonly IOfflineCommandStore _offlineStore;
        private readonly ConcurrentDictionary<Guid, CommandInfo> _commands;
        private readonly ConcurrentQueue<Guid> _pendingQueue;
        private readonly Timer _cleanupTimer;
        private readonly SemaphoreSlim _queueLock = new(1, 1);

        public event EventHandler<CommandEnqueuedEventArgs>? CommandEnqueued;
        public event EventHandler<CommandStatusUpdatedEventArgs>? CommandStatusUpdated;

        public CommandQueue(ILogger<CommandQueue> logger, IOfflineCommandStore offlineStore)
        {
            _logger = logger;
            _offlineStore = offlineStore;
            _commands = new ConcurrentDictionary<Guid, CommandInfo>();
            _pendingQueue = new ConcurrentQueue<Guid>();

            // Start cleanup timer for old completed commands
            _cleanupTimer = new Timer(CleanupOldCommands, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public async Task EnqueueCommandAsync<T>(T command) where T : class
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var commandId = Guid.NewGuid();
            var commandInfo = new CommandInfo
            {
                CommandId = commandId,
                TenantId = ExtractTenantId(command),
                AgentId = ExtractAgentId(command),
                CommandType = command.GetType().Name,
                SerializedCommand = System.Text.Json.JsonSerializer.Serialize(command),
                Status = CommandStatus.Queued,
                QueuedAt = DateTime.UtcNow,
                Timeout = ExtractTimeout(command),
                MaxRetries = ExtractMaxRetries(command)
            };

            try
            {
                await _queueLock.WaitAsync();
                _commands.TryAdd(commandId, commandInfo);
                _pendingQueue.Enqueue(commandId);

                // Save to offline store for persistence
                _ = _offlineStore.SaveCommandAsync(commandInfo);

                _logger.LogInformation("Command {CommandId} of type {CommandType} enqueued for tenant {TenantId}",
                    commandId, commandInfo.CommandType, commandInfo.TenantId);

                CommandEnqueued?.Invoke(this, new CommandEnqueuedEventArgs { Command = commandInfo });
            }
            finally
            {
                _queueLock.Release();
            }
        }

        public async Task<CommandInfo?> DequeueCommandAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_pendingQueue.TryDequeue(out var commandId))
                {
                    if (_commands.TryGetValue(commandId, out var command))
                    {
                        if (command.Status == CommandStatus.Queued)
                        {
                            // Mark as processing
                            await UpdateCommandStatusAsync(commandId, CommandStatus.Processing);
                            return command;
                        }
                        else
                        {
                            // Command status changed, skip it
                            continue;
                        }
                    }
                }

                // No commands available, wait a bit
                await Task.Delay(100, cancellationToken);
            }

            return null;
        }

        public async Task<CommandInfo?> GetCommandAsync(Guid commandId, CancellationToken cancellationToken = default)
        {
            _commands.TryGetValue(commandId, out var command);
            return command;
        }

        public async Task UpdateCommandStatusAsync(Guid commandId, CommandStatus status, object? response = null)
        {
            if (_commands.TryGetValue(commandId, out var command))
            {
                var previousStatus = command.Status;
                command.Status = status;

                switch (status)
                {
                    case CommandStatus.Processing:
                        command.StartedAt = DateTime.UtcNow;
                        break;
                    case CommandStatus.Completed:
                    case CommandStatus.Failed:
                    case CommandStatus.TimedOut:
                    case CommandStatus.Cancelled:
                        command.CompletedAt = DateTime.UtcNow;
                        break;
                }

                if (response != null)
                {
                    command.SerializedResponse = System.Text.Json.JsonSerializer.Serialize(response);
                }

                // Update status in offline store
                _ = _offlineStore.UpdateCommandStatusAsync(commandId, status.ToString());

                _logger.LogInformation("Command {CommandId} status updated from {PreviousStatus} to {CurrentStatus}",
                    commandId, previousStatus, status);

                CommandStatusUpdated?.Invoke(this, new CommandStatusUpdatedEventArgs
                {
                    Command = command,
                    PreviousStatus = previousStatus
                });
            }
            else
            {
                _logger.LogWarning("Command {CommandId} not found for status update", commandId);
            }
        }

        public async Task<List<CommandInfo>> GetPendingCommandsAsync()
        {
            return _commands.Values
                .Where(c => c.Status == CommandStatus.Queued)
                .OrderBy(c => c.QueuedAt)
                .ToList();
        }

        public async Task CancelCommandAsync(Guid commandId)
        {
            if (_commands.TryGetValue(commandId, out var command))
            {
                if (command.Status == CommandStatus.Queued || command.Status == CommandStatus.Processing)
                {
                    await UpdateCommandStatusAsync(commandId, CommandStatus.Cancelled);
                    _logger.LogInformation("Command {CommandId} cancelled", commandId);
                }
                else
                {
                    _logger.LogWarning("Cannot cancel command {CommandId} - status is {Status}",
                        commandId, command.Status);
                }
            }
        }

        public async Task<List<CommandInfo>> GetCommandsByStatusAsync(CommandStatus status)
        {
            return _commands.Values
                .Where(c => c.Status == status)
                .OrderByDescending(c => c.QueuedAt)
                .ToList();
        }

        public async Task<List<CommandInfo>> GetCommandsForDeviceAsync(string deviceId)
        {
            // This would require parsing the serialized command to extract device info
            // For now, return all commands
            return _commands.Values
                .OrderByDescending(c => c.QueuedAt)
                .ToList();
        }

        public async Task<QueueStatistics> GetStatisticsAsync()
        {
            var commands = _commands.Values.ToList();

            return new QueueStatistics
            {
                TotalCommands = commands.Count,
                PendingCommands = commands.Count(c => c.Status == CommandStatus.Queued),
                ProcessingCommands = commands.Count(c => c.Status == CommandStatus.Processing),
                CompletedCommands = commands.Count(c => c.Status == CommandStatus.Completed),
                FailedCommands = commands.Count(c => c.Status == CommandStatus.Failed),
                TimedOutCommands = commands.Count(c => c.Status == CommandStatus.TimedOut),
                CancelledCommands = commands.Count(c => c.Status == CommandStatus.Cancelled),
                OldestPendingCommand = commands
                    .Where(c => c.Status == CommandStatus.Queued)
                    .Min(c => c.QueuedAt),
                AverageProcessingTime = CalculateAverageTimeSpan(
                    commands
                        .Where(c => c.ProcessingDuration.HasValue)
                        .Select(c => c.ProcessingDuration.Value))
            };
        }

        public async Task<int> ClearCompletedCommandsAsync(TimeSpan olderThan)
        {
            var cutoffTime = DateTime.UtcNow - olderThan;
            var commandsToRemove = _commands.Values
                .Where(c => c.Status == CommandStatus.Completed && c.CompletedAt.HasValue && c.CompletedAt.Value < cutoffTime)
                .Select(c => c.CommandId)
                .ToList();

            foreach (var commandId in commandsToRemove)
            {
                _commands.TryRemove(commandId, out _);
                // Also delete from offline store
                _ = _offlineStore.DeleteCommandAsync(commandId);
            }

            _logger.LogInformation("Cleared {Count} completed commands older than {OlderThan}",
                commandsToRemove.Count, olderThan);

            return commandsToRemove.Count;
        }

        private void CleanupOldCommands(object? state)
        {
            try
            {
                _ = Task.Run(async () =>
                {
                    await ClearCompletedCommandsAsync(TimeSpan.FromHours(24));
                    // Also cleanup old commands from offline store (older than 7 days)
                    _ = await _offlineStore.CleanupOldCommandsAsync(7);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during command cleanup");
            }
        }

        #region Helper Methods

        private Guid ExtractTenantId(object command)
        {
            // Use reflection to extract TenantId property
            var property = command.GetType().GetProperty("TenantId");
            return property?.GetValue(command) as Guid? ?? Guid.Empty;
        }

        private string ExtractAgentId(object command)
        {
            // For now, return a default agent ID
            // In a real implementation, this would be extracted from the command or context
            return Environment.MachineName;
        }

        private TimeSpan ExtractTimeout(object command)
        {
            // Use reflection to extract Timeout property or return default
            var property = command.GetType().GetProperty("Timeout");
            return property?.GetValue(command) as TimeSpan? ?? TimeSpan.FromMinutes(2);
        }

        private int ExtractMaxRetries(object command)
        {
            // Return default max retries
            return 3;
        }

        private TimeSpan CalculateAverageTimeSpan(IEnumerable<TimeSpan> timespans)
        {
            var list = timespans.ToList();
            if (list.Count == 0)
                return TimeSpan.Zero;

            var averageTicks = list.Average(ts => ts.Ticks);
            return TimeSpan.FromTicks((long)averageTicks);
        }

        #endregion

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            _queueLock?.Dispose();
        }
    }
}