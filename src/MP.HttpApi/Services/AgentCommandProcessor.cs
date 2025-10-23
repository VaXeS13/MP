using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using MP.LocalAgent.Contracts.Responses;
using MP.LocalAgent.Contracts.Models;
using MP.HttpApi.Hubs;

namespace MP.Services
{
    /// <summary>
    /// Service for processing agent commands
    /// </summary>
    public class AgentCommandProcessor : IAgentCommandProcessor
    {
        private readonly ILogger<AgentCommandProcessor> _logger;
        private readonly ConcurrentDictionary<Guid, CommandExecutionStatus> _commands;
        private readonly ConcurrentDictionary<(Guid TenantId, string AgentId), Queue<Guid>> _agentQueues;
        private readonly Timer _cleanupTimer;

        public AgentCommandProcessor(ILogger<AgentCommandProcessor> logger)
        {
            _logger = logger;
            _commands = new ConcurrentDictionary<Guid, CommandExecutionStatus>();
            _agentQueues = new ConcurrentDictionary<(Guid, string), Queue<Guid>>();

            // Start cleanup timer
            _cleanupTimer = new Timer(CleanupOldCommands, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public async Task QueueTerminalCommandAsync(Guid tenantId, string agentId, string commandType, object commandData, TimeSpan timeout)
        {
            var commandId = Guid.NewGuid();
            var commandStatus = new CommandExecutionStatus
            {
                CommandId = commandId,
                TenantId = tenantId,
                AgentId = agentId,
                CommandType = commandType,
                Status = MP.LocalAgent.Contracts.Enums.CommandStatus.Queued,
                QueuedAt = DateTime.UtcNow,
                Timeout = timeout
            };

            _commands.TryAdd(commandId, commandStatus);

            // Add to agent queue
            var queueKey = (tenantId, agentId);
            _agentQueues.AddOrUpdate(queueKey,
                new Queue<Guid>(new[] { commandId }),
                (key, existingQueue) =>
                {
                    existingQueue.Enqueue(commandId);
                    return existingQueue;
                });

            _logger.LogInformation("Terminal command {CommandType} queued for agent {AgentId}, command {CommandId}",
                commandType, agentId, commandId);
        }

        public async Task QueueFiscalPrinterCommandAsync(Guid tenantId, string agentId, string commandType, object commandData, TimeSpan timeout)
        {
            var commandId = Guid.NewGuid();
            var commandStatus = new CommandExecutionStatus
            {
                CommandId = commandId,
                TenantId = tenantId,
                AgentId = agentId,
                CommandType = commandType,
                Status = MP.LocalAgent.Contracts.Enums.CommandStatus.Queued,
                QueuedAt = DateTime.UtcNow,
                Timeout = timeout
            };

            _commands.TryAdd(commandId, commandStatus);

            // Add to agent queue
            var queueKey = (tenantId, agentId);
            _agentQueues.AddOrUpdate(queueKey,
                new Queue<Guid>(new[] { commandId }),
                (key, existingQueue) =>
                {
                    existingQueue.Enqueue(commandId);
                    return existingQueue;
                });

            _logger.LogInformation("Fiscal printer command {CommandType} queued for agent {AgentId}, command {CommandId}",
                commandType, agentId, commandId);
        }

        public async Task ProcessCommandResponseAsync(Guid tenantId, string agentId, CommandResponseBase response)
        {
            _logger.LogInformation("Processing command response from agent {AgentId} for command {CommandId}",
                agentId, response.CommandId);

            try
            {
                if (_commands.TryGetValue(response.CommandId, out var commandStatus))
                {
                    commandStatus.Status = response.Success ? MP.LocalAgent.Contracts.Enums.CommandStatus.Completed : MP.LocalAgent.Contracts.Enums.CommandStatus.Failed;
                    commandStatus.CompletedAt = DateTime.UtcNow;
                    commandStatus.Response = response;

                    if (!response.Success)
                    {
                        commandStatus.ErrorMessage = response.ErrorMessage;
                        commandStatus.RetryCount++;

                        // Check if we should retry
                        if (commandStatus.RetryCount < 3)
                        {
                            _logger.LogInformation("Retrying command {CommandId} (attempt {RetryCount})",
                                response.CommandId, commandStatus.RetryCount);
                            await RetryCommandAsync(response.CommandId);
                        }
                        else
                        {
                            _logger.LogWarning("Command {CommandId} failed after {RetryCount} attempts",
                                response.CommandId, commandStatus.RetryCount);
                        }
                    }

                    _logger.LogInformation("Command response processed successfully for {CommandId}",
                        response.CommandId);
                }
                else
                {
                    _logger.LogWarning("Command {CommandId} not found for response processing", response.CommandId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing command response for {CommandId}", response.CommandId);
            }
        }

        public async Task CancelCommandAsync(Guid tenantId, string agentId, Guid commandId)
        {
            _logger.LogInformation("Cancelling command {CommandId} for agent {AgentId}", commandId, agentId);

            try
            {
                if (_commands.TryGetValue(commandId, out var commandStatus))
                {
                    commandStatus.Status = MP.LocalAgent.Contracts.Enums.CommandStatus.Cancelled;
                    commandStatus.CompletedAt = DateTime.UtcNow;

                    // Remove from queue
                    var queueKey = (tenantId, agentId);
                    if (_agentQueues.TryGetValue(queueKey, out var queue))
                    {
                        lock (queue)
                        {
                            var tempQueue = new Queue<Guid>();
                            while (queue.Count > 0)
                            {
                                var queuedCommandId = queue.Dequeue();
                                if (queuedCommandId != commandId)
                                    tempQueue.Enqueue(queuedCommandId);
                            }
                            // Rebuild queue
                            while (tempQueue.Count > 0)
                                queue.Enqueue(tempQueue.Dequeue());
                        }
                    }

                    _logger.LogInformation("Command {CommandId} cancelled successfully", commandId);
                }
                else
                {
                    _logger.LogWarning("Command {CommandId} not found for cancellation", commandId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling command {CommandId}", commandId);
            }
        }

        public async Task<CommandExecutionStatus?> GetCommandStatusAsync(Guid commandId)
        {
            _commands.TryGetValue(commandId, out var status);
            return status;
        }

        public async Task<List<QueuedCommandInfo>> GetPendingCommandsAsync(Guid tenantId, string agentId)
        {
            var queueKey = (tenantId, agentId);
            if (!_agentQueues.TryGetValue(queueKey, out var queue))
            {
                return new List<QueuedCommandInfo>();
            }

            lock (queue)
            {
                var pendingCommands = new List<QueuedCommandInfo>();
                var commandArray = queue.ToArray();

                foreach (var commandId in commandArray)
                {
                    if (_commands.TryGetValue(commandId, out var commandStatus) &&
                        commandStatus.Status == MP.LocalAgent.Contracts.Enums.CommandStatus.Queued)
                    {
                        pendingCommands.Add(new QueuedCommandInfo
                        {
                            CommandId = commandStatus.CommandId,
                            CommandType = commandStatus.CommandType,
                            Status = commandStatus.Status,
                            QueuedAt = commandStatus.QueuedAt,
                            Timeout = commandStatus.Timeout,
                            SerializedData = null // Could add serialized data if needed
                        });
                    }
                }

                return pendingCommands;
            }
        }

        public async Task<int> CleanupCompletedCommandsAsync(TimeSpan olderThan)
        {
            var cutoffTime = DateTime.UtcNow - olderThan;
            var commandsToRemove = _commands.Values
                .Where(c => c.CompletedAt.HasValue && c.CompletedAt.Value < cutoffTime)
                .Select(c => c.CommandId)
                .ToList();

            foreach (var commandId in commandsToRemove)
            {
                _commands.TryRemove(commandId, out _);
            }

            _logger.LogInformation("Cleaned up {Count} completed commands older than {OlderThan}",
                commandsToRemove.Count, olderThan);

            return commandsToRemove.Count;
        }

        private async Task RetryCommandAsync(Guid commandId)
        {
            if (_commands.TryGetValue(commandId, out var commandStatus))
            {
                commandStatus.Status = MP.LocalAgent.Contracts.Enums.CommandStatus.Queued;
                commandStatus.StartedAt = null;
                commandStatus.CompletedAt = null;
                commandStatus.ErrorMessage = null;

                // Re-add to queue
                var queueKey = (commandStatus.TenantId, commandStatus.AgentId);
                _agentQueues.AddOrUpdate(queueKey,
                    new Queue<Guid>(new[] { commandId }),
                    (key, existingQueue) =>
                    {
                        lock (existingQueue)
                        {
                            existingQueue.Enqueue(commandId);
                        }
                        return existingQueue;
                    });

                _logger.LogInformation("Command {CommandId} re-queued for retry", commandId);
            }
        }

        private void CleanupOldCommands(object? state)
        {
            try
            {
                _ = Task.Run(async () =>
                {
                    await CleanupCompletedCommandsAsync(TimeSpan.FromHours(24));
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during command cleanup");
            }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }
    }
}