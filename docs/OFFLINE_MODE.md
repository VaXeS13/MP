# Offline Mode - Local Agent Operation Without Internet

**Status**: In Development (MP-69)
**Version**: 1.0.0
**Last Updated**: 2025-10-23

---

## Overview

The MP Local Agent must support operation when Internet connection is temporarily unavailable. Customers can still:

✅ Scan items
✅ Add to cart
✅ Process cash payments
❌ Process card payments (buffered, executed when online)
✅ Print non-fiscal receipts

When connection is restored, all buffered commands are automatically executed.

---

## Offline Architecture

```
Local Agent State Machine
    ↓
ONLINE (connected to Azure)
    ↓ [Internet disconnected]
OFFLINE (buffering commands)
    ↓ [Internet restored]
SYNCING (executing buffered commands)
    ↓
ONLINE (fully synchronized)
```

---

## Command Buffering

### What Gets Buffered

**Critical Commands** (execute immediately or queue):
- Terminal payment authorization
- Fiscal receipt printing
- Z-report generation

**Non-Critical Commands** (execute immediately):
- Status checks
- Heartbeat
- Logging

### Offline Queue Storage (SQLite)

```sql
-- Offline command store (sqlite database)
CREATE TABLE OfflineCommands (
    CommandId TEXT PRIMARY KEY,              -- UUID
    CommandType TEXT NOT NULL,               -- "AuthorizePayment"
    CommandData TEXT NOT NULL,               -- JSON data
    TenantId TEXT NOT NULL,                  -- UUID
    AgentId TEXT NOT NULL,                   -- "shop-warsaw-001"
    QueuedAt TEXT NOT NULL,                  -- ISO 8601 datetime
    Timeout INTEGER NOT NULL,                -- Seconds
    RetryCount INTEGER DEFAULT 0,            -- How many times retried
    Priority INTEGER DEFAULT 0,              -- 0=low, 1=medium, 2=high
    Status TEXT DEFAULT 'Pending'            -- Pending/Executing/Completed/Failed
);

CREATE INDEX idx_queued_at ON OfflineCommands(QueuedAt);
CREATE INDEX idx_priority ON OfflineCommands(Priority DESC);
CREATE INDEX idx_status ON OfflineCommands(Status);
```

### Database Location

```
Windows:  C:\ProgramData\MP\LocalAgent\offline_queue.db
Linux:    /var/lib/mp/local-agent/offline_queue.db
MacOS:    /Library/Application Support/MP/LocalAgent/offline_queue.db
```

### Database Encryption

```csharp
// Enable SQLite encryption (requires SQLCipher)
var connectionString = @"Data Source=offline_queue.db;
    Password=encryption_key_from_config;
    Mode=rwc;";

// Or use Azure Key Vault for encryption key
var encryptionKey = await _keyVaultClient.GetSecretAsync(
    "offline-queue-encryption-key");
```

---

## Implementation

### 1. IOfflineCommandStore Interface

```csharp
namespace MP.LocalAgent.Persistence
{
    public interface IOfflineCommandStore
    {
        /// <summary>
        /// Save a command to offline storage for later execution
        /// </summary>
        Task SaveCommandAsync(CommandInfo command);

        /// <summary>
        /// Load all pending commands from storage
        /// </summary>
        Task<List<CommandInfo>> LoadAllCommandsAsync();

        /// <summary>
        /// Load commands by status
        /// </summary>
        Task<List<CommandInfo>> LoadCommandsByStatusAsync(
            string status, int maxResults = 100);

        /// <summary>
        /// Remove a successfully executed command
        /// </summary>
        Task RemoveCommandAsync(Guid commandId);

        /// <summary>
        /// Update command status (Pending -> Executing -> Completed/Failed)
        /// </summary>
        Task UpdateCommandStatusAsync(Guid commandId, string status,
            string? errorMessage = null, object? response = null);

        /// <summary>
        /// Clear all commands (use with caution!)
        /// </summary>
        Task ClearAsync();

        /// <summary>
        /// Get command count by status
        /// </summary>
        Task<int> GetCountAsync(string? status = null);

        /// <summary>
        /// Cleanup old commands (> 7 days)
        /// </summary>
        Task<int> CleanupOldCommandsAsync(TimeSpan olderThan);
    }
}
```

### 2. SQLite Implementation

```csharp
public class SqliteOfflineCommandStore : IOfflineCommandStore
{
    private readonly string _dbPath;
    private readonly ILogger<SqliteOfflineCommandStore> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);  // Thread safety

    public SqliteOfflineCommandStore(
        IOptions<LocalAgentConfiguration> config,
        ILogger<SqliteOfflineCommandStore> logger)
    {
        _dbPath = config.Value.OfflineQueueDbPath
            ?? GetDefaultDbPath();
        _logger = logger;

        InitializeDatabase();
    }

    private string GetDefaultDbPath()
    {
        var baseDir = Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "MP", "LocalAgent"),
            _ => "/var/lib/mp/local-agent"
        };

        Directory.CreateDirectory(baseDir);
        return Path.Combine(baseDir, "offline_queue.db");
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS OfflineCommands (
                CommandId TEXT PRIMARY KEY,
                CommandType TEXT NOT NULL,
                CommandData TEXT NOT NULL,
                TenantId TEXT NOT NULL,
                AgentId TEXT NOT NULL,
                QueuedAt TEXT NOT NULL,
                Timeout INTEGER NOT NULL,
                RetryCount INTEGER DEFAULT 0,
                Priority INTEGER DEFAULT 0,
                Status TEXT DEFAULT 'Pending',
                ErrorMessage TEXT,
                Response TEXT
            );

            CREATE INDEX IF NOT EXISTS idx_status
                ON OfflineCommands(Status);
            CREATE INDEX IF NOT EXISTS idx_priority
                ON OfflineCommands(Priority DESC);
            CREATE INDEX IF NOT EXISTS idx_queued_at
                ON OfflineCommands(QueuedAt);
        ";

        cmd.ExecuteNonQuery();
        _logger.LogInformation("Offline command store initialized at {Path}", _dbPath);
    }

    public async Task SaveCommandAsync(CommandInfo command)
    {
        await _semaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO OfflineCommands
                (CommandId, CommandType, CommandData, TenantId, AgentId,
                 QueuedAt, Timeout, RetryCount, Priority, Status)
                VALUES (@id, @type, @data, @tenantId, @agentId,
                        @queuedAt, @timeout, @retry, @priority, @status)
            ";

            cmd.Parameters.AddWithValue("@id", command.CommandId.ToString());
            cmd.Parameters.AddWithValue("@type", command.CommandType);
            cmd.Parameters.AddWithValue("@data",
                JsonSerializer.Serialize(command.CommandData));
            cmd.Parameters.AddWithValue("@tenantId", command.TenantId.ToString());
            cmd.Parameters.AddWithValue("@agentId", command.AgentId);
            cmd.Parameters.AddWithValue("@queuedAt",
                command.QueuedAt.ToString("O"));
            cmd.Parameters.AddWithValue("@timeout",
                (int)command.Timeout.TotalSeconds);
            cmd.Parameters.AddWithValue("@retry", command.RetryCount);
            cmd.Parameters.AddWithValue("@priority", command.Priority);
            cmd.Parameters.AddWithValue("@status", "Pending");

            await cmd.ExecuteNonQueryAsync();

            _logger.LogDebug("Saved command {CommandId} ({CommandType}) to offline store",
                command.CommandId, command.CommandType);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<CommandInfo>> LoadAllCommandsAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var commands = new List<CommandInfo>();

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM OfflineCommands
                WHERE Status IN ('Pending', 'Failed')
                ORDER BY Priority DESC, QueuedAt ASC
            ";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                commands.Add(new CommandInfo
                {
                    CommandId = Guid.Parse(reader.GetString(0)),
                    CommandType = reader.GetString(1),
                    CommandData = JsonSerializer.Deserialize<object>(
                        reader.GetString(2))!,
                    TenantId = Guid.Parse(reader.GetString(3)),
                    AgentId = reader.GetString(4),
                    QueuedAt = DateTime.Parse(reader.GetString(5)),
                    Timeout = TimeSpan.FromSeconds(reader.GetInt32(6)),
                    RetryCount = reader.GetInt32(7),
                    Priority = reader.GetInt32(8)
                });
            }

            _logger.LogInformation("Loaded {Count} commands from offline store",
                commands.Count);
            return commands;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task RemoveCommandAsync(Guid commandId)
    {
        await _semaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM OfflineCommands WHERE CommandId = @id";
            cmd.Parameters.AddWithValue("@id", commandId.ToString());

            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UpdateCommandStatusAsync(Guid commandId, string status,
        string? errorMessage = null, object? response = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE OfflineCommands
                SET Status = @status, ErrorMessage = @error, Response = @response
                WHERE CommandId = @id
            ";

            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@error", errorMessage ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@response",
                response != null ? JsonSerializer.Serialize(response) :
                (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", commandId.ToString());

            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<int> CleanupOldCommandsAsync(TimeSpan olderThan)
    {
        await _semaphore.WaitAsync();
        try
        {
            var cutoffDate = DateTime.UtcNow - olderThan;

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                DELETE FROM OfflineCommands
                WHERE Status = 'Completed' AND QueuedAt < @cutoff
            ";
            cmd.Parameters.AddWithValue("@cutoff", cutoffDate.ToString("O"));

            var deleted = await cmd.ExecuteNonQueryAsync();

            _logger.LogInformation("Cleaned up {Count} old commands from store",
                deleted);
            return deleted;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

### 3. CommandQueue Integration

```csharp
public class CommandQueue : ICommandQueue
{
    private readonly IOfflineCommandStore _offlineStore;
    private readonly ConcurrentQueue<CommandInfo> _queue;
    private readonly ILogger<CommandQueue> _logger;

    public CommandQueue(
        IOfflineCommandStore offlineStore,
        ILogger<CommandQueue> logger)
    {
        _offlineStore = offlineStore;
        _queue = new ConcurrentQueue<CommandInfo>();
        _logger = logger;

        // Load previously saved commands
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var savedCommands = await _offlineStore.LoadAllCommandsAsync();

            foreach (var cmd in savedCommands)
            {
                _queue.Enqueue(cmd);
            }

            _logger.LogInformation(
                "Initialized queue with {Count} saved commands",
                savedCommands.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to initialize queue from offline store");
        }
    }

    public async Task EnqueueCommandAsync<T>(T command) where T : class
    {
        var commandInfo = new CommandInfo
        {
            CommandId = Guid.NewGuid(),
            CommandType = typeof(T).Name,
            CommandData = command,
            QueuedAt = DateTime.UtcNow,
            Priority = DeterminePriority(typeof(T))
        };

        _queue.Enqueue(commandInfo);

        // ✅ SAVE TO OFFLINE STORE
        try
        {
            await _offlineStore.SaveCommandAsync(commandInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to save command {CommandId} to offline store",
                commandInfo.CommandId);
        }
    }

    public async Task<CommandInfo?> DequeueCommandAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_queue.TryDequeue(out var command))
            return null;

        try
        {
            // ✅ REMOVE FROM OFFLINE STORE AFTER EXECUTION
            await _offlineStore.RemoveCommandAsync(command.CommandId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to remove command {CommandId} from offline store",
                command.CommandId);
        }

        return command;
    }

    private int DeterminePriority(Type commandType) => commandType.Name switch
    {
        "AuthorizeTerminalPaymentCommand" => 2,  // High priority
        "PrintFiscalReceiptCommand" => 2,        // High priority
        "CheckTerminalStatusCommand" => 0,       // Low priority
        _ => 1  // Default: medium
    };
}
```

---

## Offline Workflow

### Scenario: Customer Pays with Card While Offline

```
1. Customer arrives to checkout
   ↓
2. Agent scans items (works - local DB)
   ↓
3. Cart total shown (works - local calculation)
   ↓
4. Customer wants to pay with card
   ↓
5. Agent starts payment: AuthorizeTerminalPaymentCommand
   ↓
6. SignalR connection is DOWN
   ↓
7. Command saved to offline_queue.db
   ↓
8. User sees: "Payment queued. Will process when online."
   ↓
9. Customer can pay with CASH instead
   ↓
10. Internet connection restored (agent reconnects)
    ↓
11. Offline commands automatically replayed
    ↓
12. Payment terminal receives: AuthorizeTerminalPaymentCommand
    ↓
13. Payment processed
    ↓
14. Admin can see: "Payment processed from offline queue"
```

### Implementation

```csharp
public async Task<TerminalPaymentResponse> AuthorizePaymentAsync(
    AuthorizeTerminalPaymentCommand request)
{
    try
    {
        // Try to execute via SignalR (online mode)
        return await _remoteDeviceProxy.AuthorizePaymentAsync(request);
    }
    catch (HubException) when (!_signalRClient.IsConnected)
    {
        // Fallback: Save to offline queue
        _logger.LogWarning(
            "Agent offline. Queueing payment command for later processing.");

        await _commandQueue.EnqueueCommandAsync(request);

        return new TerminalPaymentResponse
        {
            CommandId = request.CommandId,
            Success = false,
            Status = "queued_offline",
            ErrorMessage = "Payment queued. Will process when online.",
            ErrorCode = "AGENT_OFFLINE_QUEUED"
        };
    }
}
```

---

## Reconnection & Sync

### Automatic Sync on Reconnect

```csharp
// AgentService.cs
private async void OnSignalRConnected(object? sender, EventArgs e)
{
    _logger.LogInformation("SignalR connection restored");

    // Trigger offline command sync
    await _commandQueue.ProcessOfflineCommandsAsync();
}

public async Task ProcessOfflineCommandsAsync()
{
    var pendingCommands = await _offlineStore.LoadAllCommandsAsync();

    if (pendingCommands.Count == 0)
    {
        _logger.LogInformation("No pending commands to sync");
        return;
    }

    _logger.LogInformation("Syncing {Count} offline commands",
        pendingCommands.Count);

    foreach (var cmd in pendingCommands)
    {
        try
        {
            await _commandQueue.EnqueueCommandAsync(cmd.CommandData);
            await _offlineStore.UpdateCommandStatusAsync(
                cmd.CommandId, "Processing");

            // Execute immediately if online
            if (_signalRClient.IsConnected)
            {
                var result = await ExecuteCommandAsync(cmd);
                await _offlineStore.UpdateCommandStatusAsync(
                    cmd.CommandId, "Completed", response: result);

                await _offlineStore.RemoveCommandAsync(cmd.CommandId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process offline command {CommandId}",
                cmd.CommandId);

            await _offlineStore.UpdateCommandStatusAsync(
                cmd.CommandId, "Failed", errorMessage: ex.Message);
        }
    }

    _logger.LogInformation("Offline sync completed");
}
```

---

## Configuration

### appsettings.json

```json
{
  "LocalAgent": {
    "OfflineQueueDbPath": "C:\\ProgramData\\MP\\LocalAgent\\offline_queue.db",
    "MaxOfflineQueueSize": 10000,
    "OfflineQueueRetention": "00:07:00:00",  // 7 days
    "AutoCleanupInterval": "01:00:00"        // 24 hours
  }
}
```

### Configuration Class

```csharp
public class LocalAgentConfiguration
{
    public string? OfflineQueueDbPath { get; set; }
    public int MaxOfflineQueueSize { get; set; } = 10000;
    public TimeSpan OfflineQueueRetention { get; set; } = TimeSpan.FromDays(7);
    public TimeSpan AutoCleanupInterval { get; set; } = TimeSpan.FromHours(24);
}
```

---

## Monitoring & Debugging

### Check Queue Status

```bash
# View queue contents (SQLite)
sqlite3 offline_queue.db "SELECT CommandType, Status, COUNT(*) FROM OfflineCommands GROUP BY CommandType, Status;"

# Example output:
# CommandType | Status | COUNT
# AuthorizePayment | Pending | 3
# PrintFiscal | Completed | 1
# CheckStatus | Failed | 0
```

### Logs

```
[08:45:00] Agent offline - Queueing: AuthorizePayment (cmd_id: abc-123)
[08:45:05] Offline queue size: 3 commands
[08:52:30] SignalR connection restored
[08:52:31] Syncing 3 offline commands
[08:52:35] Processed offline command cmd_id: abc-123 (SUCCESS)
[08:52:36] Offline sync completed
```

---

## Performance Considerations

### Queue Size Limits

```csharp
if (_queue.Count >= config.MaxOfflineQueueSize)
{
    throw new OfflineQueueFullException(
        $"Offline queue full ({config.MaxOfflineQueueSize} commands). " +
        "Cannot queue more commands until online.");
}
```

### Memory Management

- In-memory queue uses concurrent collection
- SQLite database handles persistence
- Automatic cleanup removes commands > 7 days old
- Memory footprint: ~1KB per queued command

### Database Size

- Average command size: 500 bytes
- 10,000 commands = ~5MB
- With indices: ~10MB

---

## Testing

### Unit Test: Command Persistence

```csharp
[Fact]
public async Task SavedCommands_Should_Survive_Agent_Restart()
{
    // Arrange
    var store = new SqliteOfflineCommandStore(options, logger);
    var command = new CommandInfo { /* ... */ };

    // Act
    await store.SaveCommandAsync(command);

    // Simulate agent restart - create new store instance
    var newStore = new SqliteOfflineCommandStore(options, logger);
    var loaded = await newStore.LoadAllCommandsAsync();

    // Assert
    Assert.Contains(loaded, x => x.CommandId == command.CommandId);
}
```

### Integration Test: Offline → Online Sync

```csharp
[Fact]
public async Task Offline_Commands_Should_Execute_On_Reconnect()
{
    // 1. Go offline
    // 2. Queue payment command
    // 3. Verify in SQLite
    // 4. Go online
    // 5. Verify command executed
    // 6. Verify removed from queue
}
```

---

## Troubleshooting

### Queue is full

```
Error: OfflineQueueFullException - Queue has 10000 commands

Solution:
1. Check why agent couldn't connect (network?)
2. Clear old commands: await store.CleanupOldCommandsAsync(TimeSpan.FromDays(7))
3. Manually remove failed commands: DELETE FROM OfflineCommands WHERE Status = 'Failed'
```

### Commands not executing

```
Error: Commands stuck in "Processing" state

Solution:
1. Check agent logs for execution errors
2. Verify terminal/printer availability
3. Increase timeout: RemoteDeviceProxyOptions.CommandTimeout
4. Manually retry: UPDATE OfflineCommands SET Status = 'Pending'
```

---

## Summary

Offline mode ensures:

✅ Customers can still shop when online
✅ Critical commands (payments, receipts) are never lost
✅ Automatic sync when connection restored
✅ Full audit trail of what happened offline
✅ Manual recovery tools for admin

For more details:
- `docs/PCI_DSS_COMPLIANCE.md` - Secure offline storage
- `docs/AGENT_AUTHENTICATION.md` - API key verification
