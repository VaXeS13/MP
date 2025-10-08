using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Volo.Abp.MultiTenancy;

namespace MP.Middleware
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantMiddleware> _logger;

        public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ICurrentTenant currentTenant)
        {
            var host = context.Request.Host.Host;
            var subdomain = context.Items["ClientSubdomain"] as string;

            _logger.LogInformation("=== TENANT MIDDLEWARE ===");
            _logger.LogInformation("Host: {Host}", host);
            _logger.LogInformation("Detected Subdomain: {Subdomain}", subdomain ?? "none");
            _logger.LogInformation("Current Tenant ID: {TenantId}", currentTenant.Id?.ToString() ?? "null");
            _logger.LogInformation("Current Tenant Name: {TenantName}", currentTenant.Name ?? "null");
            _logger.LogInformation("Is Available: {IsAvailable}", currentTenant.IsAvailable);
            _logger.LogInformation("=========================");

            // Dodaj headers dla frontendu
            if (currentTenant.Id.HasValue)
            {
                context.Response.Headers.Add("X-Tenant-Id", currentTenant.Id.ToString());
                context.Response.Headers.Add("X-Tenant-Name", currentTenant.Name ?? "");
            }

            await _next(context);
        }
    }
}