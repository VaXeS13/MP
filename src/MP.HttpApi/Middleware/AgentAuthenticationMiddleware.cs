using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MP.Domain.LocalAgent;

namespace MP.HttpApi.Middleware
{
    /// <summary>
    /// Middleware for agent API Key authentication
    /// Validates API Key in X-Agent-ApiKey header for SignalR connections
    /// </summary>
    public class AgentAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AgentAuthenticationMiddleware> _logger;

        public AgentAuthenticationMiddleware(RequestDelegate next, ILogger<AgentAuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IServiceProvider serviceProvider)
        {
            // Only validate for agent endpoints (SignalR hubs)
            if (!context.Request.Path.StartsWithSegments("/signalr/local-agent"))
            {
                await _next(context);
                return;
            }

            try
            {
                // Extract required headers
                if (!context.Request.Headers.TryGetValue("X-Agent-ApiKey", out var apiKeyHeader))
                {
                    _logger.LogWarning("Missing API Key header for agent authentication");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { error = "Missing X-Agent-ApiKey header" });
                    return;
                }

                if (!context.Request.Headers.TryGetValue("Tenant-Id", out var tenantIdHeader))
                {
                    _logger.LogWarning("Missing Tenant-Id header for agent authentication");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { error = "Missing Tenant-Id header" });
                    return;
                }

                if (!context.Request.Headers.TryGetValue("Agent-Id", out var agentIdHeader))
                {
                    _logger.LogWarning("Missing Agent-Id header for agent authentication");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { error = "Missing Agent-Id header" });
                    return;
                }

                var apiKey = apiKeyHeader.ToString();
                var tenantIdStr = tenantIdHeader.ToString();
                var agentId = agentIdHeader.ToString();

                // Validate Tenant ID format
                if (!Guid.TryParse(tenantIdStr, out var tenantId))
                {
                    _logger.LogWarning("Invalid Tenant-Id format");
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid Tenant-Id format" });
                    return;
                }

                // Get client IP address for whitelist check
                var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                // Authenticate the API key
                var isValid = await AuthenticateApiKeyAsync(
                    serviceProvider,
                    apiKey,
                    tenantId,
                    agentId,
                    clientIp,
                    context);

                if (!isValid)
                {
                    return;
                }

                // Store authenticated agent info in context for later use
                context.Items["AgentId"] = agentId;
                context.Items["TenantId"] = tenantId;
                context.Items["ClientIp"] = clientIp;

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in agent authentication middleware");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = "Authentication service error" });
            }
        }

        private async Task<bool> AuthenticateApiKeyAsync(
            IServiceProvider serviceProvider,
            string apiKey,
            Guid tenantId,
            string agentId,
            string clientIp,
            HttpContext context)
        {
            using var scope = serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAgentApiKeyRepository>();

            // Hash the provided API key to look it up
            var keyHash = HashApiKey(apiKey);

            var storedKey = await repository.FindByKeyHashAsync(keyHash, tenantId);

            // API key not found or doesn't belong to tenant
            if (storedKey == null)
            {
                _logger.LogWarning(
                    "API Key authentication failed: key not found for tenant {TenantId}",
                    tenantId);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid API Key" });
                return false;
            }

            // Verify Agent ID matches
            if (storedKey.AgentId != agentId)
            {
                _logger.LogWarning(
                    "API Key authentication failed: Agent ID mismatch. Expected {ExpectedAgent}, got {ProvidedAgent}",
                    storedKey.AgentId,
                    agentId);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Agent ID mismatch" });
                return false;
            }

            // Check if API key is expired
            if (storedKey.IsExpired)
            {
                _logger.LogWarning(
                    "API Key authentication failed: key expired for agent {AgentId}",
                    agentId);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "API Key expired" });
                return false;
            }

            // Check if API key is active
            if (!storedKey.IsActive)
            {
                _logger.LogWarning(
                    "API Key authentication failed: key inactive for agent {AgentId}",
                    agentId);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "API Key inactive" });
                return false;
            }

            // Check if API key is locked
            if (storedKey.IsLocked)
            {
                _logger.LogWarning(
                    "API Key authentication failed: key locked for agent {AgentId} until {UnlockedAt}",
                    agentId,
                    storedKey.LockedUntil);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsJsonAsync(new { error = "API Key locked due to failed attempts" });
                return false;
            }

            // Check IP whitelist
            if (!storedKey.IsIpAllowed(clientIp))
            {
                _logger.LogWarning(
                    "API Key authentication failed: IP {ClientIp} not whitelisted for agent {AgentId}",
                    clientIp,
                    agentId);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "IP address not whitelisted" });
                return false;
            }

            // Record successful authentication
            storedKey.RecordSuccessfulAttempt();
            await repository.UpdateAsync(storedKey);

            _logger.LogInformation(
                "API Key authentication successful for agent {AgentId} from {ClientIp}",
                agentId,
                clientIp);

            return true;
        }

        /// <summary>
        /// Hash the API key using SHA256
        /// The actual API key is never stored, only its hash
        /// </summary>
        private string HashApiKey(string apiKey)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
                return Convert.ToHexString(hashedBytes);
            }
        }
    }
}
