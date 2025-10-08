using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace MP.Middleware
{
    public class SubdomainClientMiddleware
    {
        private readonly RequestDelegate _next;

        public SubdomainClientMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            string subdomain = null;

            // Metoda 1: Z query string - client_id
            if (context.Request.Query.TryGetValue("client_id", out var clientId))
            {
                // MP_App_KISS -> KISS
                if (clientId.ToString().StartsWith("MP_App_"))
                {
                    subdomain = clientId.ToString().Replace("MP_App_", "");
                }
            }
            if (!string.IsNullOrEmpty(subdomain))
            {
                context.Items["ClientSubdomain"] = subdomain.ToUpper();
                // Dodaj do logów dla debugowania
                var logger = context.RequestServices.GetService<ILogger<SubdomainClientMiddleware>>();
                logger?.LogInformation($"Detected subdomain: {subdomain} for path: {context.Request.Path}");
            }
            await _next(context);
        }
    }
}
