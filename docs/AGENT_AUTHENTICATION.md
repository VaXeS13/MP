# Agent Authentication & Authorization Guide

**Status**: In Development (MP-67)
**Version**: 1.0.0
**Last Updated**: 2025-10-23

---

## Overview

The MP Local Agent must authenticate with Azure API using secure API Keys. This prevents unauthorized agents from accessing payment processing and fiscal printing capabilities.

## Architecture

```
Local Agent (Shop Computer)
    ↓
Creates SignalR connection with API Key
    ↓
Azure SignalR Hub (LocalAgentHub)
    ↓
AgentAuthenticationMiddleware validates key
    ↓
Agent registered and authorized
    ↓
Can execute terminal & fiscal commands
```

---

## API Key System

### Key Generation

Each agent gets a unique API Key:

```csharp
// Example: Generated during agent setup
AgentId: "shop-warsaw-001"
TenantId: "12345678-1234-1234-1234-123456789012"
ApiKey: "mp_agent_1e3f5g7h9k1l3m5n7p9r1s3t5v7x9z"  // Generated
```

### Key Format

```
mp_agent_{32_random_alphanumeric_characters}
│         └─ 32 chars = 192 bits of entropy
└─ Prefix for easy identification
```

### Key Storage

**On Local Agent**:
```json
{
  "LocalAgent": {
    "TenantId": "12345678-1234-1234-1234-123456789012",
    "AgentId": "shop-warsaw-001",
    "ApiKey": "mp_agent_1e3f5g7h9k1l3m5n7p9r1s3t5v7x9z"
  }
}
```

⚠️ **IMPORTANT**: API Key must be:
- Stored in secure vault (Azure Key Vault in production)
- NOT committed to Git
- Encrypted at rest
- Rotated every 90 days

---

## Authentication Flow

### 1. Agent Startup

```csharp
// LocalAgent/Program.cs
public async Task Main(string[] args)
{
    var config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build();

    var tenantId = Guid.Parse(config["LocalAgent:TenantId"]);
    var agentId = config["LocalAgent:AgentId"];
    var apiKey = config["LocalAgent:ApiKey"];  // From secure vault

    var agentService = serviceProvider.GetRequiredService<IAgentService>();
    await agentService.InitializeAsync(tenantId, agentId);

    var signalRClient = serviceProvider.GetRequiredService<ISignalRClientService>();
    await signalRClient.ConnectAsync(
        serverUrl: "https://mp.azurewebsites.net/signalr/local-agent",
        tenantId: tenantId,
        agentId: agentId,
        apiKey: apiKey);  // ✅ Pass API Key
}
```

### 2. SignalR Connection with API Key

```csharp
// LocalAgent/Services/SignalRClientService.cs
public async Task ConnectAsync(string serverUrl, Guid tenantId,
    string agentId, string apiKey)
{
    var connection = new HubConnectionBuilder()
        .WithUrl(serverUrl, options =>
        {
            // Send API Key in header
            options.Headers.Add("X-Agent-ApiKey", apiKey);
            options.Headers.Add("Tenant-Id", tenantId.ToString());
            options.Headers.Add("Agent-Id", agentId);
        })
        .WithAutomaticReconnect(new[]
        {
            TimeSpan.Zero,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30)
        })
        .Build();

    await connection.StartAsync();
}
```

### 3. Middleware Validation

```csharp
// MP.HttpApi/Middleware/AgentAuthenticationMiddleware.cs
public async Task InvokeAsync(HttpContext context,
    IRepository<AgentApiKey, Guid> apiKeyRepository)
{
    // Only validate for agent endpoints
    if (!context.Request.Path.StartsWithSegments("/signalr/local-agent"))
    {
        await _next(context);
        return;
    }

    // Step 1: Extract headers
    if (!context.Request.Headers.TryGetValue("X-Agent-ApiKey", out var apiKeyHeader))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Missing API Key header");
        return;
    }

    if (!context.Request.Headers.TryGetValue("Tenant-Id", out var tenantIdHeader) ||
        !Guid.TryParse(tenantIdHeader, out var tenantId))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Invalid Tenant-Id");
        return;
    }

    if (!context.Request.Headers.TryGetValue("Agent-Id", out var agentIdHeader))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Missing Agent-Id");
        return;
    }

    // Step 2: Find API Key in database
    var storedApiKey = await apiKeyRepository.FirstOrDefaultAsync(x =>
        x.TenantId == tenantId &&
        x.AgentId == agentIdHeader.ToString() &&
        x.IsActive &&
        DateTime.UtcNow < x.ExpiresAt);

    if (storedApiKey == null)
    {
        _logger.LogWarning("Agent {AgentId} API key not found for tenant {TenantId}",
            agentIdHeader, tenantId);
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Invalid API Key");
        return;
    }

    // Step 3: Validate API Key (SHA256 hash comparison)
    if (!storedApiKey.ValidateApiKey(apiKeyHeader.ToString()))
    {
        _logger.LogWarning("Invalid API Key provided for agent {AgentId}",
            agentIdHeader);
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Invalid API Key");
        return;
    }

    // Step 4: IP Whitelist check (optional)
    if (!string.IsNullOrEmpty(storedApiKey.IpWhitelist))
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        var allowedIps = storedApiKey.IpWhitelist.Split(',').Select(x => x.Trim());

        if (!allowedIps.Contains(clientIp))
        {
            _logger.LogWarning("IP {Ip} not whitelisted for agent {AgentId}",
                clientIp, agentIdHeader);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("IP not whitelisted");
            return;
        }
    }

    // Step 5: Update last used timestamp
    storedApiKey.LastUsedAt = DateTime.UtcNow;
    await apiKeyRepository.UpdateAsync(storedApiKey);

    await _next(context);
}
```

### 4. Hub Authorization

```csharp
// MP.HttpApi/Hubs/LocalAgentHub.cs
[Authorize]  // JWT token still required for basic auth
public class LocalAgentHub : Hub
{
    private Guid GetTenantId()
    {
        // Middleware already validated Tenant-Id header
        var tenantIdHeader = Context.GetHttpContext()?
            .Request.Headers["Tenant-Id"].ToString();

        if (string.IsNullOrEmpty(tenantIdHeader) ||
            !Guid.TryParse(tenantIdHeader, out var tenantId))
        {
            throw new UnauthorizedAccessException("Invalid tenant context");
        }

        return tenantId;
    }

    private string GetAgentId()
    {
        return Context.GetHttpContext()?
            .Request.Headers["Agent-Id"].ToString()
            ?? throw new UnauthorizedAccessException("Missing Agent-Id");
    }

    public override async Task OnConnectedAsync()
    {
        var tenantId = GetTenantId();
        var agentId = GetAgentId();

        // Middleware already validated API key
        await _connectionManager.RegisterAgentAsync(
            tenantId, agentId, Context.ConnectionId, Context.UserIdentifier);

        await base.OnConnectedAsync();
    }
}
```

---

## API Key Database Entity

```csharp
// MP.Domain/LocalAgent/AgentApiKey.cs
public class AgentApiKey : AuditedAggregateRoot<Guid>, IMultiTenant
{
    /// <summary>
    /// Tenant ID (multi-tenancy support)
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Agent identifier (e.g., "shop-warsaw-001")
    /// </summary>
    public string AgentId { get; set; } = null!;

    /// <summary>
    /// SHA256 hash of the API key (never store plain key!)
    /// </summary>
    public string ApiKeyHash { get; set; } = null!;

    /// <summary>
    /// Salt for hashing (unique per key)
    /// </summary>
    public string ApiKeySalt { get; set; } = null!;

    /// <summary>
    /// When the key expires (90 days default)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Is this key currently active?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When was this key last used?
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Optional IP whitelist (comma-separated IPs)
    /// Example: "192.168.1.100,10.0.0.50"
    /// </summary>
    public string? IpWhitelist { get; set; }

    /// <summary>
    /// Optional description (for admin UI)
    /// Example: "Shop Warsaw Terminal #3"
    /// </summary>
    public string? Description { get; set; }

    protected AgentApiKey() { }

    public AgentApiKey(Guid id, Guid? tenantId, string agentId,
        string plainApiKey, DateTime expiresAt) : base(id)
    {
        TenantId = tenantId;
        AgentId = agentId;
        ExpiresAt = expiresAt;
        IsActive = true;

        // Generate salt and hash
        ApiKeySalt = GenerateSalt();
        ApiKeyHash = HashApiKey(plainApiKey, ApiKeySalt);
    }

    /// <summary>
    /// Validate if the provided API key matches the stored hash
    /// </summary>
    public bool ValidateApiKey(string providedApiKey)
    {
        // Compute hash of provided key with stored salt
        var hash = HashApiKey(providedApiKey, ApiKeySalt);

        // Time-constant comparison (prevents timing attacks)
        return CryptographicHash.Equals(hash, ApiKeyHash) &&
               IsActive &&
               DateTime.UtcNow < ExpiresAt;
    }

    /// <summary>
    /// Revoke this API key
    /// </summary>
    public void Revoke()
    {
        IsActive = false;
    }

    /// <summary>
    /// Renew this API key (extend expiration)
    /// </summary>
    public void Renew(DateTime newExpiresAt)
    {
        ExpiresAt = newExpiresAt;
        IsActive = true;
    }

    private static string GenerateSalt()
    {
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var saltBytes = new byte[32];
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    private static string HashApiKey(string apiKey, string salt)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var combined = apiKey + salt;
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hashBytes);
    }
}
```

---

## Configuration

### Local Agent (appsettings.json)

```json
{
  "LocalAgent": {
    "TenantId": "12345678-1234-1234-1234-123456789012",
    "AgentId": "shop-warsaw-001",
    "ApiKey": "mp_agent_1e3f5g7h9k1l3m5n7p9r1s3t5v7x9z",
    "ServerUrl": "https://mp.azurewebsites.net/signalr/local-agent",
    "ConnectionTimeout": 30000,
    "ReconnectMaxAttempts": 5
  }
}
```

### Azure (appsettings.Production.json)

```json
{
  "LocalAgent": {
    "TenantId": "TODO",
    "AgentId": "TODO",
    "ApiKey": "TODO_FROM_KEYVAULT",
    "ServerUrl": "https://mp.azurewebsites.net/signalr/local-agent"
  },
  "KeyVault": {
    "Enabled": true,
    "VaultUrl": "https://mp-keyvault.vault.azure.net/",
    "ClientId": "TODO",
    "ClientSecret": "TODO"
  }
}
```

---

## API Key Lifecycle

### 1. Generation

Admin generates new key for an agent:

```csharp
// Admin endpoint (TODO: implement)
[HttpPost("agents/{agentId}/api-keys")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<CreateApiKeyResponse>> CreateApiKey(
    Guid tenantId, string agentId, CreateApiKeyRequest request)
{
    var plainApiKey = GenerateRandomApiKey();  // Returns something like: mp_agent_xyz...

    var apiKeyEntity = new AgentApiKey(
        Guid.NewGuid(),
        tenantId,
        agentId,
        plainApiKey,
        DateTime.UtcNow.AddDays(90));  // 90-day expiration

    await _apiKeyRepository.InsertAsync(apiKeyEntity);

    return new CreateApiKeyResponse
    {
        ApiKey = plainApiKey,  // ⚠️ Return ONLY ONCE - user must save
        ExpiresAt = apiKeyEntity.ExpiresAt,
        Message = "⚠️ Save this API key securely. You won't be able to see it again."
    };
}
```

### 2. Distribution

Agent operator saves the key to local config:
```json
// appsettings.json on shop computer
{
  "LocalAgent": {
    "ApiKey": "mp_agent_1e3f5g7h9k1l3m5n7p9r1s3t5v7x9z"
  }
}
```

### 3. Usage

Agent uses key to connect to Azure API (middleware validates it).

### 4. Monitoring

Admin monitors API key usage:

```csharp
[HttpGet("agents/{agentId}/api-keys")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<List<ApiKeyStatusDto>>> GetApiKeys(
    Guid tenantId, string agentId)
{
    var keys = await _apiKeyRepository.GetListAsync(x =>
        x.TenantId == tenantId && x.AgentId == agentId);

    return keys.Select(k => new ApiKeyStatusDto
    {
        Id = k.Id,
        IsActive = k.IsActive,
        CreatedAt = k.CreationTime,
        ExpiresAt = k.ExpiresAt,
        LastUsedAt = k.LastUsedAt,
        DaysUntilExpiry = (int)(k.ExpiresAt - DateTime.UtcNow).TotalDays,
        IsExpired = DateTime.UtcNow >= k.ExpiresAt
    }).ToList();
}
```

### 5. Rotation (every 90 days)

```csharp
// Scheduled job to notify admins of expiring keys
[DisableConcurrentExecution]
public class ApiKeyExpirationNotificationJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var expiringKeys = await _apiKeyRepository.GetListAsync(x =>
            x.IsActive &&
            x.ExpiresAt <= DateTime.UtcNow.AddDays(14) &&
            x.ExpiresAt > DateTime.UtcNow);

        foreach (var key in expiringKeys)
        {
            await _notificationService.SendAsync(
                userId: key.CreatorId,  // Notify who created it
                message: $"API key for agent {key.AgentId} expires in " +
                        $"{(key.ExpiresAt - DateTime.UtcNow).TotalDays:F0} days. " +
                        $"Please rotate it.");
        }
    }
}
```

### 6. Revocation

```csharp
[HttpDelete("agents/{agentId}/api-keys/{keyId}")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult> RevokeApiKey(
    Guid tenantId, string agentId, Guid keyId)
{
    var apiKey = await _apiKeyRepository.GetAsync(keyId);

    if (apiKey.TenantId != tenantId || apiKey.AgentId != agentId)
        throw new UnauthorizedAccessException();

    apiKey.Revoke();
    await _apiKeyRepository.UpdateAsync(apiKey);

    return Ok();
}
```

---

## Security Best Practices

### ✅ DO

- ✅ Store keys in secure vault (Azure Key Vault)
- ✅ Rotate keys every 90 days
- ✅ Use IP whitelist for additional security
- ✅ Log all API key usage
- ✅ Revoke immediately if compromised
- ✅ Use HTTPS/TLS for all connections
- ✅ Hash keys with salt before storage

### ❌ DON'T

- ❌ Commit API keys to Git
- ❌ Log plain API keys
- ❌ Use same key for multiple agents
- ❌ Share keys via email
- ❌ Store keys in plain text
- ❌ Use weak keys (<32 characters)
- ❌ Allow unlimited key expiration

---

## Testing

### Unit Tests

```csharp
[Fact]
public void ValidateApiKey_Should_Accept_Correct_Key()
{
    // Arrange
    var plainKey = "mp_agent_1234567890abcdefghijklmnop";
    var apiKey = new AgentApiKey(Guid.NewGuid(), Guid.NewGuid(),
        "test-agent", plainKey, DateTime.UtcNow.AddDays(90));

    // Act
    var isValid = apiKey.ValidateApiKey(plainKey);

    // Assert
    Assert.True(isValid);
}

[Fact]
public void ValidateApiKey_Should_Reject_Wrong_Key()
{
    // Arrange
    var plainKey = "mp_agent_1234567890abcdefghijklmnop";
    var apiKey = new AgentApiKey(Guid.NewGuid(), Guid.NewGuid(),
        "test-agent", plainKey, DateTime.UtcNow.AddDays(90));

    // Act
    var isValid = apiKey.ValidateApiKey("wrong_key");

    // Assert
    Assert.False(isValid);
}

[Fact]
public void ValidateApiKey_Should_Reject_Expired_Key()
{
    // Arrange
    var plainKey = "mp_agent_1234567890abcdefghijklmnop";
    var apiKey = new AgentApiKey(Guid.NewGuid(), Guid.NewGuid(),
        "test-agent", plainKey, DateTime.UtcNow.AddSeconds(-1));  // Expired

    // Act
    var isValid = apiKey.ValidateApiKey(plainKey);

    // Assert
    Assert.False(isValid);
}

[Fact]
public void ValidateApiKey_Should_Reject_Inactive_Key()
{
    // Arrange
    var plainKey = "mp_agent_1234567890abcdefghijklmnop";
    var apiKey = new AgentApiKey(Guid.NewGuid(), Guid.NewGuid(),
        "test-agent", plainKey, DateTime.UtcNow.AddDays(90));
    apiKey.Revoke();

    // Act
    var isValid = apiKey.ValidateApiKey(plainKey);

    // Assert
    Assert.False(isValid);
}
```

### Integration Tests

```csharp
[Fact]
public async Task LocalAgentHub_Should_Reject_Missing_ApiKey()
{
    // Arrange
    var connection = new HubConnectionBuilder()
        .WithUrl("https://localhost:5001/signalr/local-agent")
        // ❌ No API Key header
        .Build();

    // Act & Assert
    await Assert.ThrowsAsync<HttpRequestException>(() =>
        connection.StartAsync());
}

[Fact]
public async Task LocalAgentHub_Should_Accept_Valid_ApiKey()
{
    // Arrange
    var apiKey = await CreateTestApiKey();
    var connection = new HubConnectionBuilder()
        .WithUrl("https://localhost:5001/signalr/local-agent",
            options =>
            {
                options.Headers.Add("X-Agent-ApiKey", apiKey);
                options.Headers.Add("Tenant-Id", TestTenantId.ToString());
                options.Headers.Add("Agent-Id", "test-agent");
            })
        .Build();

    // Act
    await connection.StartAsync();

    // Assert
    Assert.Equal(HubConnectionState.Connected, connection.State);
}
```

---

## Admin UI (TODO)

Future admin dashboard for managing API keys:

```
Admin Dashboard
├── Agents List
│   ├── Warsaw Shop
│   │   ├── API Keys
│   │   │   ├── Active: mp_agent_xxxx... (expires in 60 days)
│   │   │   ├── Active: mp_agent_yyyy... (expires in 30 days)
│   │   │   └── Expired: mp_agent_zzzz... [Revoked 2025-09-01]
│   │   ├── [+ Create New Key] button
│   │   └── [Rotate All Keys] button
│   └── Krakow Shop
│       └── ...
└── API Key Audit Log
    ├── 2025-10-23 10:45 - API Key "mp_agent_xxxx" used (SUCCESS)
    ├── 2025-10-23 10:32 - API Key "mp_agent_yyyy" used (FAILED - Expired)
    └── ...
```

---

## Troubleshooting

### Agent can't connect

**Error**: `401 Unauthorized`

**Possible causes**:
1. Wrong API key
2. API key expired
3. Agent ID mismatch
4. IP not whitelisted
5. API key revoked

**Solution**:
```bash
# Check API key in local config
cat appsettings.json | grep ApiKey

# Check API key status in database
SELECT * FROM AgentApiKeys WHERE AgentId = 'shop-warsaw-001'

# Regenerate key if needed
POST /api/agents/{agentId}/api-keys
```

### API key compromised

**Steps**:
1. Revoke the compromised key immediately
2. Generate a new key
3. Distribute to shop via secure channel
4. Review audit logs for unauthorized access
5. Check payment transaction logs for suspicious activity

---

## Summary

The agent authentication system ensures:

✅ Only authorized agents can connect
✅ API keys are securely hashed
✅ Keys expire automatically (90 days)
✅ IP whitelist for additional security
✅ Full audit trail of all API key usage
✅ Support for multi-tenant isolation

For more details, see:
- `docs/PCI_DSS_COMPLIANCE.md` - Payment security
- `docs/OFFLINE_MODE.md` - Offline operation
- `docs/API_KEY_MANAGEMENT.md` - Admin operations (coming soon)
