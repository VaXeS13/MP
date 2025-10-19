using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace MP.Middleware
{
    public class SubdomainDetectionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SubdomainDetectionMiddleware> _logger;

        public SubdomainDetectionMiddleware(RequestDelegate next, ILogger<SubdomainDetectionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var host = context.Request.Host.Host;
            var path = context.Request.Path;
            var method = context.Request.Method;

            // Wyciągnij subdomenę z różnych źródeł
            var subdomain = ExtractSubdomainFromRequest(context);
            var clientOrigin = GetClientOriginFromRequest(context);

            // Zapisz informacje w HttpContext.Items dla dalszych middleware
            context.Items["ClientSubdomain"] = subdomain;
            context.Items["ClientOrigin"] = clientOrigin;
            context.Items["OriginalHost"] = host;

            /*
            _logger.LogInformation("=== SUBDOMAIN DETECTION ===");
            _logger.LogInformation("Request: {Method} {Path}", method, path);
            _logger.LogInformation("Host: {Host}", host);
            _logger.LogInformation("Origin: {Origin}", context.Request.Headers["Origin"].ToString());
            _logger.LogInformation("Referer: {Referer}", context.Request.Headers["Referer"].ToString());
            _logger.LogInformation("Redirect_uri: {RedirectUri}", context.Request.Query["redirect_uri"].ToString());
            _logger.LogInformation("Detected Subdomain: {Subdomain}", subdomain ?? "none");
            _logger.LogInformation("Client Origin: {ClientOrigin}", clientOrigin ?? "none");
            _logger.LogInformation("================================");
            */
            await _next(context);
        }

        private static string? ExtractSubdomainFromRequest(HttpContext context)
        {
            // 1. Sprawdź redirect_uri (OAuth flow)
            var redirectUri = context.Request.Query["redirect_uri"].FirstOrDefault();
            if (!string.IsNullOrEmpty(redirectUri))
            {
                var subdomainFromRedirect = ExtractSubdomainFromUrl(redirectUri);
                if (!string.IsNullOrEmpty(subdomainFromRedirect))
                {
                    return subdomainFromRedirect;
                }
            }

            // 2. Sprawdź Origin header
            var origin = context.Request.Headers["Origin"].FirstOrDefault();
            if (!string.IsNullOrEmpty(origin))
            {
                var subdomainFromOrigin = ExtractSubdomainFromUrl(origin);
                if (!string.IsNullOrEmpty(subdomainFromOrigin))
                {
                    return subdomainFromOrigin;
                }
            }

            // 3. Sprawdź Referer header
            var referer = context.Request.Headers["Referer"].FirstOrDefault();
            if (!string.IsNullOrEmpty(referer))
            {
                var subdomainFromReferer = ExtractSubdomainFromUrl(referer);
                if (!string.IsNullOrEmpty(subdomainFromReferer))
                {
                    return subdomainFromReferer;
                }
            }

            return null;
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
            catch
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

        private static string GetClientOriginFromRequest(HttpContext context)
        {
            // 1. Origin header
            var origin = context.Request.Headers["Origin"].FirstOrDefault();
            if (!string.IsNullOrEmpty(origin))
            {
                return origin;
            }

            // 2. Referer header
            var referer = context.Request.Headers["Referer"].FirstOrDefault();
            if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
            {
                return $"{refererUri.Scheme}://{refererUri.Host}:{refererUri.Port}";
            }

            // 3. Redirect URI
            var redirectUri = context.Request.Query["redirect_uri"].FirstOrDefault();
            if (!string.IsNullOrEmpty(redirectUri) && Uri.TryCreate(redirectUri, UriKind.Absolute, out var redirectUriParsed))
            {
                return $"{redirectUriParsed.Scheme}://{redirectUriParsed.Host}:{redirectUriParsed.Port}";
            }

            return null;
        }
    }
}