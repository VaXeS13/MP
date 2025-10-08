using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace MP.Middleware
{
    public class PerHostCookieMiddleware
    {
        private readonly RequestDelegate _next;

        public PerHostCookieMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Wyciągnij host, np. kiss.localhost / cto.localhost
            var host = context.Request.Host.Host.Replace(".", "_");

            // Podmień nazwę cookie używanego przez Identity
            context.Features.Set<IAuthenticationFeature>(new AuthenticationFeature
            {
                OriginalPath = context.Request.Path,
                OriginalPathBase = context.Request.PathBase
            });

            var schemeProvider = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
            var handlerProvider = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();

            var scheme = await schemeProvider.GetSchemeAsync(IdentityConstants.ApplicationScheme);
            if (scheme != null)
            {
                var handler = await handlerProvider.GetHandlerAsync(context, IdentityConstants.ApplicationScheme);
                if (handler is CookieAuthenticationHandler cookieHandler)
                {
                    cookieHandler.Options.Cookie.Name = $".Auth_{host}";
                }
            }

            await _next(context);
        }
    }

}
