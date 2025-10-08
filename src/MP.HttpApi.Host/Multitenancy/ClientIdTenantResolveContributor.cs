using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.MultiTenancy;

namespace MP.MultiTenancy
{
    public class ClientIdTenantResolveContributor : TenantResolveContributorBase
    {
        public const string ContributorName = "ClientId";
        public override string Name => ContributorName;

        public override async Task ResolveAsync(ITenantResolveContext context)
        {
            var httpContext = context.GetHttpContext();
            if (httpContext == null) return;

            // Pobierz subdomenę wykrytą przez SubdomainDetectionMiddleware
            var subdomain = httpContext.Items["ClientSubdomain"] as string;
            var tenantName = httpContext.Items["TenantName"] as string;

            if (!string.IsNullOrEmpty(subdomain))
            {
                // Użyj subdomeny jako tenant name
                var resolvedTenantName = tenantName ?? subdomain;

                context.Handled = true;
                context.TenantIdOrName = resolvedTenantName;

                var logger = context.ServiceProvider.GetService(typeof(ILogger<ClientIdTenantResolveContributor>)) as ILogger<ClientIdTenantResolveContributor>;
                logger?.LogDebug("Resolved tenant {TenantName} from subdomain {Subdomain} for path {Path}",
                    resolvedTenantName, subdomain, httpContext.Request.Path);
            }
            else
            {
                var logger = context.ServiceProvider.GetService(typeof(ILogger<ClientIdTenantResolveContributor>)) as ILogger<ClientIdTenantResolveContributor>;
                logger?.LogDebug("No subdomain found for tenant resolution. Path: {Path}", httpContext.Request.Path);
            }

            await Task.CompletedTask;
        }
    }
}
