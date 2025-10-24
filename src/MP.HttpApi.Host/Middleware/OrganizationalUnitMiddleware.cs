using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Volo.Abp;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;
using MP.Domain.OrganizationalUnits;

namespace MP.HttpApi.Host.Middleware
{
    /// <summary>
    /// Middleware for resolving and setting the current organizational unit context
    /// Automatically extracts organizational unit ID from various sources and validates access
    /// </summary>
    public class OrganizationalUnitMiddleware
    {
        private const string CookieKeyCurrentOrganizationalUnitId = "CurrentOrganizationalUnitId";
        private const string HeaderKeyOrganizationalUnitId = "X-Organizational-Unit-Id";
        private const string QueryKeyOrganizationalUnitId = "unitId";

        private readonly RequestDelegate _next;

        public OrganizationalUnitMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(
            HttpContext context,
            ICurrentOrganizationalUnit currentOrganizationalUnit,
            ICurrentUser currentUser,
            ICurrentTenant currentTenant)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(currentOrganizationalUnit, nameof(currentOrganizationalUnit));

            // Try to resolve organizational unit ID from various sources
            var organizationalUnitId = ResolveOrganizationalUnitId(context);

            if (organizationalUnitId.HasValue)
            {
                try
                {
                    // Set the current organizational unit context
                    using (currentOrganizationalUnit.Change(organizationalUnitId))
                    {
                        // Set the cookie for persistence
                        SetOrganizationalUnitCookie(context, organizationalUnitId.Value);

                        // Continue with the request
                        await _next(context);
                    }
                }
                catch (CurrentOrganizationalUnitNotSetException)
                {
                    // Organizational unit was not properly set
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Invalid organizational unit",
                        message = "The specified organizational unit is not valid or not accessible"
                    });
                }
                catch (BusinessException ex) when (ex.Code == "ORG_UNIT_NOT_FOUND")
                {
                    // Organizational unit doesn't exist
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Organizational unit not found",
                        message = ex.Message
                    });
                }
                catch (BusinessException ex) when (ex.Code == "ORG_UNIT_ACCESS_DENIED")
                {
                    // User doesn't have access to this organizational unit
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Access denied",
                        message = "You don't have access to this organizational unit"
                    });
                }
            }
            else
            {
                // No organizational unit specified - continue without setting context
                // This is allowed for public endpoints or those that don't require organizational unit
                await _next(context);
            }
        }

        /// <summary>
        /// Tries to resolve the organizational unit ID from various sources in priority order
        /// </summary>
        private Guid? ResolveOrganizationalUnitId(HttpContext context)
        {
            // Priority 1: Cookie (most persistent)
            if (context.Request.Cookies.TryGetValue(CookieKeyCurrentOrganizationalUnitId, out var cookieValue))
            {
                if (Guid.TryParse(cookieValue, out var cookieId))
                {
                    return cookieId;
                }
            }

            // Priority 2: HTTP Header
            if (context.Request.Headers.TryGetValue(HeaderKeyOrganizationalUnitId, out var headerValue))
            {
                if (Guid.TryParse(headerValue.ToString(), out var headerId))
                {
                    return headerId;
                }
            }

            // Priority 3: Query Parameter
            if (context.Request.Query.TryGetValue(QueryKeyOrganizationalUnitId, out var queryValue))
            {
                if (Guid.TryParse(queryValue.ToString(), out var queryId))
                {
                    return queryId;
                }
            }

            // No organizational unit ID found
            return null;
        }

        /// <summary>
        /// Sets a cookie to persist the current organizational unit selection
        /// </summary>
        private void SetOrganizationalUnitCookie(HttpContext context, Guid organizationalUnitId)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                // Session cookie - expires when browser closes
                Expires = null,
                // Allow subdomain access for multi-tenant setup
                IsEssential = true
            };

            context.Response.Cookies.Append(
                CookieKeyCurrentOrganizationalUnitId,
                organizationalUnitId.ToString(),
                cookieOptions);
        }
    }
}
