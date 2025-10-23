using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using MP.LocalAgent.Contracts.Enums;
using MP.LocalAgent.Contracts.Models;
using MP.LocalAgent.Interfaces;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MP.LocalAgent.Persistence
{
    /// <summary>
    /// SQLite-based implementation of offline command persistence
    /// </summary>
    public class SQLiteOfflineCommandStore : IOfflineCommandStore
    {
        private readonly ILogger<SQLiteOfflineCommandStore> _logger;
        private readonly string _connectionString;
        private const string TableName = "OfflineCommands";
        private const int MaxQueueSize = 10000;

        public SQLiteOfflineCommandStore(string databasePath, ILogger<SQLiteOfflineCommandStore> logger)
        {
            _logger = logger;

            // Ensure directory exists
            var directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _connectionString = $"Data Source={databasePath};";
            _logger.LogInformation("SQLite offline command store initialized with database: {DatabasePath}", databasePath);
        }

        public async Task InitializeAsync()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = $@"
                    CREATE TABLE IF NOT EXISTS {TableName} (
                        CommandId TEXT PRIMARY KEY,
                        TenantId TEXT NOT NULL,
                        AgentId TEXT,
                        CommandType TEXT NOT NULL,
                        SerializedCommand TEXT NOT NULL,
                        Status TEXT NOT NULL,
                        QueuedAt TEXT NOT NULL,
                        StartedAt TEXT,
                        CompletedAt TEXT,
                        SerializedResponse TEXT,
                        Timeout TEXT,
                        MaxRetries INTEGER,
                        RetryCount INTEGER DEFAULT 0,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    );

                    CREATE INDEX IF NOT EXISTS idx_Status ON {TableName}(Status);
                    CREATE INDEX IF NOT EXISTS idx_TenantId ON {TableName}(TenantId);
                    CREATE INDEX IF NOT EXISTS idx_QueuedAt ON {TableName}(QueuedAt);
                    CREATE INDEX IF NOT EXISTS idx_CreatedAt ON {TableName}(CreatedAt);
                ";

                await command.ExecuteNonQueryAsync();
                _logger.LogInformation("Offline command store tables initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing offline command store");
                throw;
            }
        }

        public async Task<bool> SaveCommandAsync(CommandInfo command)
        {
            try
            {
                // Check queue size first
                var queueSize = await GetQueueSizeAsync();
                if (queueSize >= MaxQueueSize)
                {
                    _logger.LogWarning("Offline command queue is full (size: {QueueSize}), cannot save command {CommandId}",
                        queueSize, command.CommandId);
                    return false;
                }

                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var cmd = connection.CreateCommand();
                cmd.CommandText = $@"
                    INSERT OR REPLACE INTO {TableName}
                    (CommandId, TenantId, AgentId, CommandType, SerializedCommand, Status, QueuedAt, Timeout, MaxRetries)
                    VALUES (@commandId, @tenantId, @agentId, @commandType, @serializedCommand, @status, @queuedAt, @timeout, @maxRetries)
                ";

                cmd.Parameters.AddWithValue("@commandId", command.CommandId.ToString());
                cmd.Parameters.AddWithValue("@tenantId", command.TenantId.ToString());
                cmd.Parameters.AddWithValue("@agentId", command.AgentId ?? "");
                cmd.Parameters.AddWithValue("@commandType", command.CommandType);
                cmd.Parameters.AddWithValue("@serializedCommand", command.SerializedCommand);
                cmd.Parameters.AddWithValue("@status", command.Status.ToString());
                cmd.Parameters.AddWithValue("@queuedAt", command.QueuedAt.ToString("O"));
                cmd.Parameters.AddWithValue("@timeout", command.Timeout.ToString("c"));
                cmd.Parameters.AddWithValue("@maxRetries", command.MaxRetries);

                var result = await cmd.ExecuteNonQueryAsync();
                _logger.LogDebug("Command {CommandId} saved to offline store", command.CommandId);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving command {CommandId} to offline store", command.CommandId);
                return false;
            }
        }

        public async Task<List<CommandInfo>> LoadPendingCommandsAsync()
        {
            var commands = new List<CommandInfo>();

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var cmd = connection.CreateCommand();
                cmd.CommandText = $"SELECT * FROM {TableName} WHERE Status = @status ORDER BY QueuedAt ASC";
                cmd.Parameters.AddWithValue("@status", CommandStatus.Queued.ToString());

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    commands.Add(ReadCommandFromReader(reader));
                }

                _logger.LogDebug("Loaded {Count} pending commands from offline store", commands.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pending commands from offline store");
            }

            return commands;
        }

        public async Task<bool> UpdateCommandStatusAsync(Guid commandId, string status)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var cmd = connection.CreateCommand();
                cmd.CommandText = $"UPDATE {TableName} SET Status = @status WHERE CommandId = @commandId";
                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@commandId", commandId.ToString());

                var result = await cmd.ExecuteNonQueryAsync();
                _logger.LogDebug("Command {CommandId} status updated to {Status}", commandId, status);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating command {CommandId} status", commandId);
                return false;
            }
        }

        public async Task<bool> DeleteCommandAsync(Guid commandId)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var cmd = connection.CreateCommand();
                cmd.CommandText = $"DELETE FROM {TableName} WHERE CommandId = @commandId";
                cmd.Parameters.AddWithValue("@commandId", commandId.ToString());

                var result = await cmd.ExecuteNonQueryAsync();
                _logger.LogDebug("Command {CommandId} deleted from offline store", commandId);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting command {CommandId}", commandId);
                return false;
            }
        }

        public async Task<CommandInfo?> GetCommandAsync(Guid commandId)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var cmd = connection.CreateCommand();
                cmd.CommandText = $"SELECT * FROM {TableName} WHERE CommandId = @commandId";
                cmd.Parameters.AddWithValue("@commandId", commandId.ToString());

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return ReadCommandFromReader(reader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving command {CommandId}", commandId);
            }

            return null;
        }

        public async Task<int> CleanupOldCommandsAsync(int olderThanDays)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
                var cmd = connection.CreateCommand();
                cmd.CommandText = $@"
                    DELETE FROM {TableName}
                    WHERE CreatedAt < @cutoffDate
                    AND Status IN (@completed, @failed, @timedOut)
                ";
                cmd.Parameters.AddWithValue("@cutoffDate", cutoffDate.ToString("O"));
                cmd.Parameters.AddWithValue("@completed", CommandStatus.Completed.ToString());
                cmd.Parameters.AddWithValue("@failed", CommandStatus.Failed.ToString());
                cmd.Parameters.AddWithValue("@timedOut", CommandStatus.TimedOut.ToString());

                var deletedCount = await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("Cleaned up {Count} old commands from offline store (older than {Days} days)",
                    deletedCount, olderThanDays);
                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old commands");
                return 0;
            }
        }

        public async Task<int> GetQueueSizeAsync()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var cmd = connection.CreateCommand();
                cmd.CommandText = $"SELECT COUNT(*) FROM {TableName}";

                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue size");
                return 0;
            }
        }

        private CommandInfo ReadCommandFromReader(SqliteDataReader reader)
        {
            return new CommandInfo
            {
                CommandId = Guid.Parse(reader.GetString(0)),
                TenantId = Guid.Parse(reader.GetString(1)),
                AgentId = reader.GetString(2),
                CommandType = reader.GetString(3),
                SerializedCommand = reader.GetString(4),
                Status = (CommandStatus)Enum.Parse(typeof(CommandStatus), reader.GetString(5)),
                QueuedAt = DateTime.Parse(reader.GetString(6)),
                StartedAt = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7)),
                CompletedAt = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
                SerializedResponse = reader.IsDBNull(9) ? null : reader.GetString(9),
                Timeout = TimeSpan.Parse(reader.GetString(10) ?? "00:02:00"),
                MaxRetries = reader.GetInt32(11)
            };
        }

        public void Dispose()
        {
            _logger.LogInformation("SQLite offline command store disposed");
        }
    }
}
