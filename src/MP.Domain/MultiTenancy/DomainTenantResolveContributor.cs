using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Microsoft.Extensions.DependencyInjection;
namespace MP.Domain.MultiTenancy
{
    public class DomainTenantResolveContributor : TenantResolveContributorBase, ITransientDependency
    {
        public const string ContributorName = "Domain";

        public override string Name => ContributorName;

        public override async Task ResolveAsync(ITenantResolveContext context)
        {
            var httpContext = context.ServiceProvider.GetService<IHttpContextAccessor>()?.HttpContext;
            if (httpContext == null)
            {
                return;
            }

            var host = httpContext.Request.Host.Host;
            var logger = context.ServiceProvider.GetService<ILogger<DomainTenantResolveContributor>>();

            // Skip localhost bez subdomeny
            if (host == "localhost" || host.StartsWith("localhost:"))
            {
                logger?.LogDebug("Skipping tenant resolution for localhost");
                return;
            }

            // Wyciągnij subdomenę
            var subdomain = ExtractSubdomain(host);
            if (string.IsNullOrEmpty(subdomain))
            {
                logger?.LogDebug($"No subdomain found in host: {host}");
                return;
            }

            logger?.LogDebug($"Resolving tenant for subdomain: {subdomain}");

            // Znajdź tenant po nazwie (subdomena)
            var tenantRepository = context.ServiceProvider.GetRequiredService<ITenantRepository>();
            var tenant = await tenantRepository.FindByNameAsync(subdomain);

            if (tenant != null)
            {
                context.Handled = true;
                context.TenantIdOrName = tenant.Id.ToString();
                logger?.LogDebug($"Resolved tenant: {tenant.Name} (ID: {tenant.Id})");
            }
            else
            {
                logger?.LogWarning($"Tenant not found for subdomain: {subdomain}");
            }
        }

        private string ExtractSubdomain(string host)
        {
            // Przykłady:
            // tenant1.localhost → tenant1
            // shop1.mydomain.com → shop1
            // localhost → null

            if (host.Contains(".localhost"))
            {
                var parts = host.Split('.');
                return parts.Length > 1 ? parts[0] : null;
            }

            // Dla prawdziwych domen (w production)
            var domainParts = host.Split('.');
            if (domainParts.Length > 2)
            {
                return domainParts[0];
            }

            return null;
        }
    }
}