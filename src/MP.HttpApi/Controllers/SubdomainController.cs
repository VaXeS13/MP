using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;

namespace MP.Controllers
{
    [Route("api/app/subdomain")]
    [ApiController]
    public class SubdomainController : AbpController
    {
        private readonly IOpenIddictApplicationManager _applicationManager;

        public SubdomainController(IOpenIddictApplicationManager applicationManager)
        {
            _applicationManager = applicationManager;
        }

        [HttpGet("oauth-debug")]
        [AllowAnonymous] // Dla debugowania OAuth flow
        public async Task<IActionResult> GetOAuthDebugInfoAsync()
        {
            var subdomain = HttpContext.Items["ClientSubdomain"] as string;
            var clientId = HttpContext.Items["ClientId"] as string;
            var origin = HttpContext.Items["ClientOrigin"] as string;

            // Sprawdź parametry OAuth
            var oauthClientId = HttpContext.Request.Query["client_id"].FirstOrDefault();
            var redirectUri = HttpContext.Request.Query["redirect_uri"].FirstOrDefault();
            var responseType = HttpContext.Request.Query["response_type"].FirstOrDefault();
            var scope = HttpContext.Request.Query["scope"].FirstOrDefault();

            // Informacje o kliencie OAuth
            object clientInfo = null;
            if (!string.IsNullOrEmpty(oauthClientId))
            {
                var application = await _applicationManager.FindByClientIdAsync(oauthClientId);
                if (application != null)
                {
                    var displayName = await _applicationManager.GetDisplayNameAsync(application);
                    var redirectUris = await _applicationManager.GetRedirectUrisAsync(application);
                    var permissions = await _applicationManager.GetPermissionsAsync(application);

                    clientInfo = new
                    {
                        clientId = oauthClientId,
                        displayName = displayName,
                        redirectUris = redirectUris.Select(u => u.ToString()),
                        permissions = permissions,
                        exists = true
                    };
                }
                else
                {
                    clientInfo = new
                    {
                        clientId = oauthClientId,
                        exists = false,
                        error = "Client not found in database"
                    };
                }
            }

            return Ok(new
            {
                // Wykryte przez middleware
                detectedSubdomain = subdomain,
                detectedClientId = clientId,
                clientOrigin = origin,
                detectionSource = HttpContext.Items["SubdomainDetectionSource"],

                // OAuth parameters
                oauth = new
                {
                    clientId = oauthClientId,
                    redirectUri = redirectUri,
                    responseType = responseType,
                    scope = scope,
                    clientInfo = clientInfo
                },

                // Request info
                request = new
                {
                    path = HttpContext.Request.Path,
                    method = HttpContext.Request.Method,
                    queryString = HttpContext.Request.QueryString.ToString(),
                    host = HttpContext.Request.Host.ToString(),
                    isHttps = HttpContext.Request.IsHttps
                },

                // Headers
                headers = new
                {
                    origin = HttpContext.Request.Headers["Origin"].FirstOrDefault(),
                    referer = HttpContext.Request.Headers["Referer"].FirstOrDefault(),
                    userAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault(),
                    customSubdomain = HttpContext.Request.Headers["X-Client-Subdomain"].FirstOrDefault()
                },

                // Authentication
                authentication = new
                {
                    isAuthenticated = HttpContext.User?.Identity?.IsAuthenticated == true,
                    userName = HttpContext.User?.Identity?.Name,
                    claims = HttpContext.User?.Claims?.Select(c => new { c.Type, c.Value })
                }
            });
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetSubdomainInfoAsync()
        {
            // Poprzednia implementacja pozostaje bez zmian
            var subdomain = HttpContext.Items["ClientSubdomain"] as string;
            var clientId = HttpContext.Items["ClientId"] as string;
            var origin = HttpContext.Items["ClientOrigin"] as string;

            if (string.IsNullOrEmpty(subdomain))
            {
                return Ok(new
                {
                    hasSubdomain = false,
                    message = "No subdomain detected",
                    origin = origin,
                    detectionSource = HttpContext.Items["SubdomainDetectionSource"]
                });
            }

            object clientInfo = null;
            if (!string.IsNullOrEmpty(clientId))
            {
                var application = await _applicationManager.FindByClientIdAsync(clientId);
                if (application != null)
                {
                    var displayName = await _applicationManager.GetDisplayNameAsync(application);
                    var redirectUris = await _applicationManager.GetRedirectUrisAsync(application);

                    clientInfo = new
                    {
                        clientId = clientId,
                        displayName = displayName,
                        redirectUris = redirectUris.Select(u => u.ToString()),
                        isActive = true
                    };
                }
            }

            return Ok(new
            {
                hasSubdomain = true,
                subdomain = subdomain,
                clientId = clientId,
                clientInfo = clientInfo,
                origin = origin,
                detectionSource = HttpContext.Items["SubdomainDetectionSource"],
                isValidClient = clientInfo != null,
                isAuthenticated = HttpContext.User?.Identity?.IsAuthenticated == true,
                userName = HttpContext.User?.Identity?.Name
            });
        }
    }
}