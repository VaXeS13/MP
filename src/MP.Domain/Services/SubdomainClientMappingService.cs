using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace MP.Domain.Services
{
    public class SubdomainClientMappingService : ISubdomainClientMappingService, ITransientDependency
    {
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SubdomainClientMappingService> _logger;

        public SubdomainClientMappingService(
            IOpenIddictApplicationManager applicationManager,
            IMemoryCache cache,
            ILogger<SubdomainClientMappingService> logger)
        {
            _applicationManager = applicationManager;
            _cache = cache;
            _logger = logger;
        }

        public async Task<string> GetClientIdForSubdomainAsync(string subdomain)
        {
            if (string.IsNullOrEmpty(subdomain) || !IsValidSubdomain(subdomain))
                return null;

            var cacheKey = $"client_id_for_subdomain_{subdomain}";

            if (_cache.TryGetValue(cacheKey, out string cachedClientId))
            {
                return cachedClientId;
            }

            try
            {
                // Dynamic subdomain to ClientId mapping
                var clientId = GenerateClientIdFromSubdomain(subdomain);

                // Sprawdź czy klient istnieje w tabeli OpenIddictApplications
                var application = await _applicationManager.FindByClientIdAsync(clientId);

                if (application != null)
                {
                    _cache.Set(cacheKey, clientId, TimeSpan.FromMinutes(30));
                    _logger.LogDebug("Found ClientId {ClientId} for subdomain {Subdomain}", clientId, subdomain);
                    return clientId;
                }

                // Jeśli nie znaleziono, spróbuj znaleźć po RedirectUri
                var clientIdByUri = await FindClientByRedirectUriAsync(subdomain);
                if (!string.IsNullOrEmpty(clientIdByUri))
                {
                    _cache.Set(cacheKey, clientIdByUri, TimeSpan.FromMinutes(30));
                    return clientIdByUri;
                }

                _logger.LogWarning("No OAuth client found for subdomain: {Subdomain}", subdomain);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding client for subdomain: {Subdomain}", subdomain);
                return null;
            }
        }

        public async Task<SubdomainClientInfo> GetClientInfoForSubdomainAsync(string subdomain)
        {
            var clientId = await GetClientIdForSubdomainAsync(subdomain);
            if (string.IsNullOrEmpty(clientId))
                return null;

            try
            {
                var application = await _applicationManager.FindByClientIdAsync(clientId);
                if (application == null)
                    return null;

                var displayName = await _applicationManager.GetDisplayNameAsync(application);
                var redirectUris = await _applicationManager.GetRedirectUrisAsync(application);
                var postLogoutUris = await _applicationManager.GetPostLogoutRedirectUrisAsync(application);

                return new SubdomainClientInfo
                {
                    ClientId = clientId,
                    DisplayName = displayName,
                    RedirectUri = redirectUris.FirstOrDefault()?.ToString(),
                    PostLogoutRedirectUri = postLogoutUris.FirstOrDefault()?.ToString(),
                    IsActive = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting client info for subdomain: {Subdomain}", subdomain);
                return null;
            }
        }

        public string ExtractSubdomainFromOrigin(string origin)
        {
            if (string.IsNullOrEmpty(origin))
                return null;

            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                return null;

            var host = uri.Host.ToLowerInvariant();
            var parts = host.Split('.');

            // Development: cto.localhost
            if (parts.Length >= 2 && parts[^1] == "localhost")
            {
                var subdomain = parts[0];
                return IsValidSubdomain(subdomain) ? subdomain : null;
            }

            // Production: cto.mp.com
            if (parts.Length >= 3)
            {
                var subdomain = parts[0];
                var systemSubdomains = new[] { "www", "api", "auth", "admin", "mail", "cdn" };

                if (systemSubdomains.Contains(subdomain))
                    return null;

                return IsValidSubdomain(subdomain) ? subdomain : null;
            }

            return null;
        }

        public bool IsValidSubdomain(string subdomain)
        {
            if (string.IsNullOrWhiteSpace(subdomain))
                return false;

            if (subdomain.Length < 2 || subdomain.Length > 50)
                return false;

            return Regex.IsMatch(subdomain, @"^[a-z0-9]([a-z0-9\-]*[a-z0-9])?$");
        }

        private async Task<string> FindClientByRedirectUriAsync(string subdomain)
        {
            try
            {
                // Szukaj klienta, który ma RedirectUri zawierający subdomenę
                var searchPatterns = new[]
                {
                    $"http://{subdomain}.localhost",
                    $"https://{subdomain}.localhost",
                    $"http://{subdomain}.mp.com",
                    $"https://{subdomain}.mp.com"
                };

                await foreach (var application in _applicationManager.ListAsync())
                {
                    var redirectUris = await _applicationManager.GetRedirectUrisAsync(application);

                    foreach (var redirectUri in redirectUris)
                    {
                        foreach (var pattern in searchPatterns)
                        {
                            if (redirectUri.ToString().StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
                            {
                                return await _applicationManager.GetClientIdAsync(application);
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching client by redirect URI for subdomain: {Subdomain}", subdomain);
                return null;
            }
        }

        private string GenerateClientIdFromSubdomain(string subdomain)
        {
            if (string.IsNullOrEmpty(subdomain))
                return null;

            // Normalize subdomain for client_id generation
            var normalizedSubdomain = subdomain
                .ToUpperInvariant()
                .Replace("-", "_")  // Replace hyphens with underscores for OAuth compatibility
                .Replace(".", "_"); // Replace dots with underscores

            // Legacy hardcoded mappings for existing clients
            var clientId = normalizedSubdomain switch
            {
                "CTO" => "MP_App_CTO",
                "KISS" => "MP_App_KISS",
                _ => $"MP_App_{normalizedSubdomain}" // Dynamic pattern for all new tenants
            };

            _logger.LogDebug("Generated ClientId {ClientId} for subdomain {Subdomain}", clientId, subdomain);
            return clientId;
        }
    }
}
