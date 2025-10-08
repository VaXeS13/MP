using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MP.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;

namespace MP.Controllers
{
    [Route("api/subdomain")]
    [ApiController]
    public class SubdomainInfoController : AbpController
    {
        private readonly ISubdomainClientMappingService _subdomainService;

        public SubdomainInfoController(ISubdomainClientMappingService subdomainService)
        {
            _subdomainService = subdomainService;
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetSubdomainInfoAsync()
        {
            var subdomain = HttpContext.Items["ClientSubdomain"] as string;
            var clientId = HttpContext.Items["ClientId"] as string;
            var origin = HttpContext.Items["ClientOrigin"] as string;

            if (string.IsNullOrEmpty(subdomain))
            {
                return Ok(new
                {
                    HasSubdomain = false,
                    Message = "No subdomain detected",
                    Origin = origin,
                    DetectionSource = HttpContext.Items["SubdomainDetectionSource"]
                });
            }

            var clientInfo = await _subdomainService.GetClientInfoForSubdomainAsync(subdomain);

            return Ok(new
            {
                HasSubdomain = true,
                Subdomain = subdomain,
                ClientId = clientId,
                ClientInfo = clientInfo,
                Origin = origin,
                DetectionSource = HttpContext.Items["SubdomainDetectionSource"],
                IsValidClient = clientInfo != null
            });
        }

        [HttpGet("debug")]
        public IActionResult GetDebugInfoAsync()
        {
            return Ok(new
            {
                // Request Info
                RequestHost = HttpContext.Request.Host.ToString(),
                RequestPath = HttpContext.Request.Path,

                // Headers
                Origin = HttpContext.Request.Headers["Origin"].FirstOrDefault(),
                Referer = HttpContext.Request.Headers["Referer"].FirstOrDefault(),
                CustomSubdomain = HttpContext.Request.Headers["X-Client-Subdomain"].FirstOrDefault(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault(),

                // Detected Info
                DetectedSubdomain = HttpContext.Items["ClientSubdomain"],
                DetectedClientId = HttpContext.Items["ClientId"],
                ClientOrigin = HttpContext.Items["ClientOrigin"],
                DetectionSource = HttpContext.Items["SubdomainDetectionSource"],
                UnknownSubdomain = HttpContext.Items["UnknownSubdomain"],

                // Cookies (for debugging isolation)
                Cookies = HttpContext.Request.Cookies.ToDictionary(c => c.Key, c => c.Value),

                // All Headers
                AllHeaders = HttpContext.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
            });
        }
    }
}
