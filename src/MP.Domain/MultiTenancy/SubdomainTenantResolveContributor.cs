using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.MultiTenancy;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Microsoft.AspNetCore.Http;

namespace MP.MultiTenancy
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(ITenantResolveContributor))]
    public class SubdomainTenantResolveContributor : HttpTenantResolveContributorBase
    {
        public const string ContributorName = "Subdomain";

        public override string Name => ContributorName;

        protected override async Task<string?> GetTenantIdOrNameFromHttpContextOrNullAsync(
            ITenantResolveContext context,
            HttpContext httpContext)
        {
            Console.WriteLine("=== SUBDOMAIN TENANT RESOLVER START ===");

            // Sprawdź cache w HttpContext.Items
            if (httpContext.Items.TryGetValue("ResolvedTenantName", out var cachedTenant))
            {
                var cached = cachedTenant as string;
                Console.WriteLine($"Using cached tenant: {cached ?? "null"}");
                return cached;
            }

            string resolvedTenant = null;

            // 1. PRIORITY: Check __tenant query parameter (from login redirects)
            var tenantQueryParam = httpContext.Request.Query["__tenant"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantQueryParam))
            {
                resolvedTenant = tenantQueryParam.ToUpperInvariant();
                Console.WriteLine($"Resolved tenant from __tenant query param: {tenantQueryParam} -> {resolvedTenant}");
            }
            // 2. Check middleware context items (from subdomain detection)
            else if (httpContext.Items.TryGetValue("ClientSubdomain", out var existingSubdomain) &&
                existingSubdomain is string subdomain && !string.IsNullOrEmpty(subdomain))
            {
                resolvedTenant = subdomain.ToUpperInvariant();
                Console.WriteLine($"Using subdomain from middleware: {subdomain} -> {resolvedTenant}");
            }
            // 3. Fallback: Extract from redirect_uri parameter
            else
            {
                var redirectUri = httpContext.Request.Query["redirect_uri"].FirstOrDefault();
                if (!string.IsNullOrEmpty(redirectUri))
                {
                    var subdomainFromRedirect = ExtractSubdomainFromUrl(redirectUri);
                    if (!string.IsNullOrEmpty(subdomainFromRedirect))
                    {
                        resolvedTenant = subdomainFromRedirect.ToUpperInvariant();
                        Console.WriteLine($"Resolved tenant from redirect_uri: {resolvedTenant}");
                    }
                }

                // 4. Additional fallback: Check ReturnUrl for redirect_uri
                if (resolvedTenant == null)
                {
                    var returnUrl = httpContext.Request.Query["ReturnUrl"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        var decodedReturnUrl = System.Net.WebUtility.UrlDecode(returnUrl);
                        var match = System.Text.RegularExpressions.Regex.Match(decodedReturnUrl, @"redirect_uri=([^&]+)");
                        if (match.Success)
                        {
                            var encodedRedirectUri = match.Groups[1].Value;
                            var redirectUriFromReturn = System.Net.WebUtility.UrlDecode(encodedRedirectUri);
                            var subdomainFromReturn = ExtractSubdomainFromUrl(redirectUriFromReturn);
                            if (!string.IsNullOrEmpty(subdomainFromReturn))
                            {
                                resolvedTenant = subdomainFromReturn.ToUpperInvariant();
                                Console.WriteLine($"Resolved tenant from ReturnUrl redirect_uri: {subdomainFromReturn} -> {resolvedTenant}");
                            }
                        }
                    }
                }
            }

            if (resolvedTenant == null)
            {
                Console.WriteLine("No tenant resolved");
                return null;
            }

            // For now, return the tenant name instead of ID
            // ABP will handle the conversion internally
            Console.WriteLine($"Resolved tenant name: {resolvedTenant}");

            // Cache wynik (tenant name)
            httpContext.Items["ResolvedTenantName"] = resolvedTenant;
            return resolvedTenant;
        }

        private static string? ExtractSubdomainFromUrl(string url)
        {
            try
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    return ExtractSubdomain(uri.Host);
                }
            }
            catch (Exception)
            {
                // Ignore parsing errors
            }
            return null;
        }

        private static string? ExtractSubdomain(string host)
        {
            if (string.IsNullOrEmpty(host))
                return null;

            // Dla localhost (rozwój)
            if (host.Contains(".localhost"))
            {
                var parts = host.Split('.');
                if (parts.Length >= 2 && parts[0] != "www")
                {
                    return parts[0];
                }
            }
            // Dla domeny produkcyjnej (np. mp.com)
            else if (!IsMainDomain(host))
            {
                var parts = host.Split('.');
                if (parts.Length >= 3 && parts[0] != "www")
                {
                    return parts[0];
                }
            }

            return null;
        }

        private static bool IsMainDomain(string host)
        {
            return host == "localhost" ||
                   host == "mp.com" ||
                   host == "www.mp.com" ||
                   host.StartsWith("127.0.0.1") ||
                   host.StartsWith("192.168.");
        }
    }
}