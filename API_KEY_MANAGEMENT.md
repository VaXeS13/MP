# Agent API Key Management (MP-67)

## Overview

Agent API Key authentication system provides secure, multi-tenant agent communication with SHA256 cryptographic hashing, IP whitelisting, and rate limiting capabilities.

## Architecture

### Domain Layer: `AgentApiKey` Entity

Located: `src/MP.Domain/LocalAgent/AgentApiKey.cs`

**Key Properties:**
```csharp
public Guid Id { get; set; }
public Guid TenantId { get; set; }
public string AgentId { get; set; }
public string KeyHash { get; set; }                    // SHA256 hash of actual key
public string Salt { get; set; }                       // Random salt for key generation
public string? LastUsedIpAddress { get; set; }        // Audit trail
public int UsageCount { get; set; }                    // Authentication attempt counter
public DateTime? LastUsedAt { get; set; }             // Last successful authentication
public bool IsActive { get; set; }                     // Can be manually disabled
public DateTime ExpiresAt { get; set; }               // Automatic expiration date
public int FailedAuthenticationAttempts { get; set; } // Failed attempt counter
public DateTime? LockedUntil { get; set; }            // Lock duration (15 minutes)
public string? IpWhitelist { get; set; }              // Comma-separated IP addresses
public bool ShouldRotate { get; set; }                // Rotation flag for key management
public DateTime CreatedAt { get; set; }
public DateTime? UpdatedAt { get; set; }
```

### Security Features

#### 1. SHA256 Key Hashing
- Actual API keys are **never** stored in the database
- Only cryptographic hashes are persisted
- Keys use format: `mp_agent_{8-char-random}{24-char-random}`
- Prefix enables quick key type identification

#### 2. Multi-Tenant Isolation
```csharp
// Each key is bound to a specific tenant
public Guid TenantId { get; set; }
public string AgentId { get; set; }
```
- Keys cannot be used across tenants
- Tenant context validation in middleware

#### 3. IP Whitelisting
```csharp
public bool IsIpAllowed(string clientIp)
{
    if (string.IsNullOrEmpty(IpWhitelist))
        return true;  // No restrictions

    var allowedIps = IpWhitelist.Split(',')
        .Select(ip => ip.Trim())
        .ToList();

    return allowedIps.Contains(clientIp);
}
```

#### 4. Rate Limiting (Automatic Lockout)
```csharp
public void RecordFailedAttempt()
{
    FailedAuthenticationAttempts++;

    if (FailedAuthenticationAttempts >= 5)
    {
        IsLocked = true;
        LockedUntil = DateTime.UtcNow.AddMinutes(15);
    }
}

public void RecordSuccessfulAttempt()
{
    UsageCount++;
    LastUsedAt = DateTime.UtcNow;
    FailedAuthenticationAttempts = 0;  // Reset counter
    LockedUntil = null;                 // Unlock on success
}
```

### Repository Layer: `IAgentApiKeyRepository`

Located: `src/MP.Domain/LocalAgent/IAgentApiKeyRepository.cs`

**Specialized Query Methods:**

```csharp
public interface IAgentApiKeyRepository : IRepository<AgentApiKey, Guid>
{
    // Find key by hash for authentication
    Task<AgentApiKey?> FindByKeyHashAsync(string keyHash, Guid tenantId);

    // Get all active keys for an agent
    Task<List<AgentApiKey>> GetActiveKeysForAgentAsync(string agentId, Guid tenantId);

    // Find expired keys for cleanup
    Task<List<AgentApiKey>> GetExpiredKeysAsync(Guid tenantId);

    // Find keys approaching expiration for rotation warnings
    Task<List<AgentApiKey>> GetKeysNearExpirationAsync(int daysThreshold = 7);

    // Find currently locked keys
    Task<List<AgentApiKey>> GetLockedKeysAsync(Guid tenantId);

    // Find unused keys for audit
    Task<List<AgentApiKey>> GetUnusedKeysAsync(int daysThreshold = 30, Guid? tenantId = null);

    // Count active keys per agent (for limits)
    Task<int> CountActiveKeysPerAgentAsync(string agentId, Guid tenantId);
}
```

**Implementation:** `src/MP.EntityFrameworkCore/LocalAgent/EfCoreAgentApiKeyRepository.cs`

### Middleware Layer: `AgentAuthenticationMiddleware`

Located: `src/MP.HttpApi/Middleware/AgentAuthenticationMiddleware.cs`

**Responsibilities:**
1. Extract authentication headers: `X-Agent-ApiKey`, `Tenant-Id`, `Agent-Id`
2. Validate API key against stored hash
3. Check tenant context and agent ID match
4. Verify key is not expired, inactive, or locked
5. Validate client IP against whitelist
6. Record authentication attempt (success/failure)

**Request Header Format:**
```http
X-Agent-ApiKey: mp_agent_xxxxxxxxxxxxxxxxxxxxxxxx
Tenant-Id: {tenantId-uuid}
Agent-Id: agent-001
```

**Response Status Codes:**
- `200 OK`: Valid authentication
- `401 Unauthorized`: Key not found or mismatch
- `403 Forbidden`: Key expired, inactive, or IP not whitelisted
- `429 Too Many Requests`: Key locked due to failed attempts

## Key Management Procedures

### 1. Creating API Keys

**Backend Service Method:**
```csharp
public async Task<AgentApiKeyDto> CreateApiKeyAsync(
    CreateAgentApiKeyDto input)
{
    var (apiKey, keyHash, salt) = GenerateSecureApiKey();

    var agentApiKey = new AgentApiKey
    {
        TenantId = CurrentTenant.Id!.Value,
        AgentId = input.AgentId,
        KeyHash = keyHash,
        Salt = salt,
        ExpiresAt = DateTime.UtcNow.AddDays(input.ValidityDays ?? 90),
        IsActive = true,
        IpWhitelist = input.IpWhitelist  // Comma-separated
    };

    await _repository.InsertAsync(agentApiKey);

    // Return actual key only once at creation
    return new AgentApiKeyDto
    {
        Id = agentApiKey.Id,
        ApiKey = apiKey,  // Client MUST save this securely
        CreatedAt = agentApiKey.CreatedAt
    };
}

private (string apiKey, string keyHash, string salt) GenerateSecureApiKey()
{
    const string prefix = "mp_agent_";
    var randomPart = Convert.ToBase64String(
        RandomNumberGenerator.GetBytes(18));

    var apiKey = prefix + randomPart;
    var salt = Convert.ToBase64String(
        RandomNumberGenerator.GetBytes(16));

    var keyHash = HashApiKey(apiKey, salt);

    return (apiKey, keyHash, salt);
}

private string HashApiKey(string apiKey, string salt)
{
    using var sha256 = SHA256.Create();
    var combined = apiKey + salt;
    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
    return Convert.ToBase64String(hash);
}
```

### 2. Key Expiration and Rotation

**Identifying Keys for Rotation:**
```csharp
// Get keys expiring within 7 days
var keysToRotate = await _repository
    .GetKeysNearExpirationAsync(daysThreshold: 7);

foreach (var key in keysToRotate)
{
    // Log warning for agent operators
    _logger.LogWarning(
        "API Key {KeyId} for agent {AgentId} expires in {Days} days",
        key.Id, key.AgentId,
        (key.ExpiresAt - DateTime.UtcNow).Days);
}
```

**Rotation Process:**
1. Create new key for agent
2. Update agent code to use new key (no downtime required)
3. Set old key `ExpiresAt` to immediate
4. Monitor old key usage (should drop to zero)
5. Delete old key after grace period (24 hours)

### 3. IP Whitelist Management

**Setting IP Whitelist:**
```csharp
var updateDto = new UpdateAgentApiKeyDto
{
    IpWhitelist = "192.168.1.1,192.168.1.2,10.0.0.0/24"
};

agentApiKey.IpWhitelist = updateDto.IpWhitelist;
await _repository.UpdateAsync(agentApiKey);
```

**Empty Whitelist = No Restrictions**
```csharp
// Allow from any IP
agentApiKey.IpWhitelist = null;

// Restrict to specific IPs
agentApiKey.IpWhitelist = "10.0.0.0/24,203.0.113.0";
```

### 4. Handling Failed Attempts and Lockouts

**Lock Mechanism:**
- After 5 consecutive failed authentication attempts, key is locked
- Lock duration: 15 minutes
- Lock is automatically cleared on next successful authentication
- Failed attempts counter is reset on success

**Monitoring Locked Keys:**
```csharp
var lockedKeys = await _repository
    .GetLockedKeysAsync(tenantId);

foreach (var key in lockedKeys)
{
    var remainingLockTime = key.LockedUntil!.Value - DateTime.UtcNow;
    _logger.LogWarning(
        "API Key {KeyId} locked for {Minutes} more minutes",
        key.Id, remainingLockTime.TotalMinutes);
}
```

### 5. Disabling Keys

```csharp
agentApiKey.IsActive = false;
await _repository.UpdateAsync(agentApiKey);

// Subsequent authentication attempts will be rejected
```

## Integration with SignalR Hubs

The middleware is registered in the HTTP pipeline to validate agent connections:

**Configuration (MPHttpApiHostModule.cs):**
```csharp
public override void OnApplicationInitialization(
    ApplicationInitializationContext context)
{
    var app = context.GetApplicationBuilder();

    // ... other middleware ...

    // Agent API Key authentication for SignalR connections
    app.UseMiddleware<AgentAuthenticationMiddleware>();

    app.MapSignalRHubs();
}
```

**SignalR Hub Registration:**
```csharp
app.MapHub<LocalAgentHub>("/hubs/agent");
```

**Client Connection Code:**
```javascript
// JavaScript/TypeScript client
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/agent", {
        headers: {
            "X-Agent-ApiKey": apiKey,
            "Tenant-Id": tenantId,
            "Agent-Id": agentId
        }
    })
    .build();

await connection.start();
```

## Testing

**Test File:** `test/MP.Application.Tests/LocalAgent/AgentAuthenticationTests.cs`

**Test Coverage (13 scenarios):**

1. **Valid Authentication** - Key passes all validations
2. **Usage Recording** - UsageCount and LastUsedAt are updated
3. **Expiration** - Expired keys are rejected
4. **Near-Expiration** - Keys can be identified for rotation
5. **Active/Inactive** - Inactive keys are rejected
6. **IP Whitelist Allowed** - Request from whitelisted IP succeeds
7. **IP Whitelist Empty** - Empty whitelist allows all IPs
8. **IP Whitelist Denied** - Request from non-whitelisted IP fails
9. **Locking** - Key locks after 5 failed attempts
10. **Lock Clearing** - Successful auth clears lock
11. **Agent ID Mismatch** - Key bound to wrong agent is rejected
12. **Key Rotation Detection** - Keys near expiration are identified
13. **Repository Queries** - Specialized query methods work correctly

**Running Tests:**
```bash
dotnet test test/MP.Application.Tests/MP.Application.Tests.csproj \
    --filter "AgentAuthenticationTests" -v q
```

## Security Best Practices

### For Developers

1. **Never Log API Keys**
   ```csharp
   // ❌ BAD
   _logger.LogInformation("API Key: {ApiKey}", apiKey);

   // ✅ GOOD
   _logger.LogInformation("API Key created for agent {AgentId}", agentId);
   ```

2. **Use HTTPS Only**
   - API keys must only be transmitted over HTTPS
   - Middleware validates request security context

3. **Hash Keys Before Storage**
   - Never store plaintext keys
   - Always use SHA256 with salt

4. **Rotate Keys Regularly**
   - Set expiration dates (recommend: 90 days)
   - Monitor key usage patterns
   - Disable unused keys

### For Operations

1. **Key Distribution**
   - Transmit keys through secure channels (password manager, encrypted email)
   - Never share keys in plain text chat or logs
   - Revoke keys immediately if compromised

2. **Monitoring**
   - Track authentication failures by key
   - Alert on repeated failed attempts (potential brute force)
   - Monitor key creation and deletion events
   - Review usage patterns for anomalies

3. **Audit Trail**
   - `LastUsedAt` - When key was last used
   - `LastUsedIpAddress` - Source IP of last authentication
   - `UsageCount` - Total authentication attempts
   - `FailedAuthenticationAttempts` - Current failure count

4. **Incident Response**
   - Suspected compromise: Set `IsActive = false` immediately
   - Lost key: Create new key, disable old one
   - Stolen key: Lock key, investigate access logs
   - Brute force: Monitor lock status, extend lock duration if needed

## Common Issues and Troubleshooting

### Issue: Key Authentication Fails (401)

**Causes:**
1. Key hash doesn't match - typo in key or wrong key
2. Key doesn't exist - key was deleted
3. Agent ID mismatch - key bound to different agent

**Solution:**
```csharp
var key = await _repository.FindByKeyHashAsync(keyHash, tenantId);
if (key == null)
{
    _logger.LogWarning("Key not found for tenant {TenantId}", tenantId);
    return Unauthorized();
}
```

### Issue: IP Whitelist Blocking Valid Requests (403)

**Causes:**
1. Agent's public IP changed (especially for cloud agents)
2. Proxy/load balancer not forwarding client IP
3. Typo in whitelist

**Solution:**
```csharp
// Verify client IP detection
var clientIp = httpContext.Connection.RemoteIpAddress?.ToString();
_logger.LogInformation("Client IP: {ClientIp}, Whitelist: {Whitelist}",
    clientIp, agentApiKey.IpWhitelist);

// Update whitelist if IP changed
agentApiKey.IpWhitelist = "new-ip-address";
```

### Issue: Key Locked After Failed Attempts (429)

**Causes:**
1. Wrong API key being used
2. Tenant ID mismatch
3. Key was revoked or expired

**Solution:**
```csharp
var lockedKey = await _repository.GetAsync(keyId);

if (lockedKey.IsLocked && lockedKey.LockedUntil > DateTime.UtcNow)
{
    var waitTime = lockedKey.LockedUntil.Value - DateTime.UtcNow;
    _logger.LogWarning(
        "Key locked. Retry in {Minutes} minutes",
        waitTime.TotalMinutes);

    // Either wait or create new key
}
```

### Issue: Key Expiration Not Preventing Auth

**Causes:**
1. Expiration validation logic missing
2. SystemClock is off
3. Test data has future expiration

**Solution:**
```csharp
// Verify expiration is checked
if (agentApiKey.IsExpired)  // Checks: ExpiresAt < DateTime.UtcNow
{
    _logger.LogWarning("Key {KeyId} has expired", agentApiKey.Id);
    return Unauthorized();
}

// Check system time
_logger.LogInformation("Current UTC time: {UtcNow}", DateTime.UtcNow);
```

## Metrics and Monitoring

**Key Metrics to Track:**

```sql
-- Authentication attempts by key
SELECT
    Id,
    AgentId,
    UsageCount,
    FailedAuthenticationAttempts,
    LastUsedAt
FROM AgentApiKeys
ORDER BY UsageCount DESC

-- Locked keys
SELECT
    Id,
    AgentId,
    LockedUntil,
    FailedAuthenticationAttempts
FROM AgentApiKeys
WHERE IsLocked = 1

-- Keys approaching expiration
SELECT
    Id,
    AgentId,
    ExpiresAt,
    DATEDIFF(day, GETDATE(), ExpiresAt) AS DaysUntilExpiration
FROM AgentApiKeys
WHERE ExpiresAt <= DATEADD(day, 7, GETDATE())
ORDER BY ExpiresAt ASC

-- Unused keys (not accessed in 30 days)
SELECT
    Id,
    AgentId,
    LastUsedAt,
    CreatedAt,
    IsActive
FROM AgentApiKeys
WHERE LastUsedAt IS NULL
   OR LastUsedAt <= DATEADD(day, -30, GETDATE())
ORDER BY LastUsedAt ASC
```

## API Endpoints (Future)

Planned endpoints for key management:

```http
POST   /api/app/agent-api-keys              # Create new key
GET    /api/app/agent-api-keys              # List keys for agent
GET    /api/app/agent-api-keys/{id}         # Get key details
PATCH  /api/app/agent-api-keys/{id}         # Update (disable, whitelist)
DELETE /api/app/agent-api-keys/{id}         # Delete key
POST   /api/app/agent-api-keys/{id}/rotate  # Create new key, disable old
```

## Related Issues and Links

- **MP-67**: Agent Authentication - API Keys and JWT
- **MP-62**: IRemoteDeviceProxy implementation
- **MP-63**: ItemCheckoutAppService integration
- **MP-65**: End-to-end testing

## Changelog

### Version 1.0.0 (MP-67 Complete)
- Initial release with complete authentication system
- 13 integration tests
- SHA256 key hashing
- Multi-tenant isolation
- IP whitelisting
- Rate limiting with 15-minute lockout
- Comprehensive audit logging
