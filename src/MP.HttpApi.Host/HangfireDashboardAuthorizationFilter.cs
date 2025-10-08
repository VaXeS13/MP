using Hangfire.Dashboard;

namespace MP
{
    /// <summary>
    /// Authorization filter for Hangfire Dashboard
    /// In production, implement proper authorization checks
    /// </summary>
    public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            // TODO: In production, implement proper authorization
            // For example: check if user is authenticated and has admin role
            // var httpContext = context.GetHttpContext();
            // return httpContext.User.Identity?.IsAuthenticated == true &&
            //        httpContext.User.IsInRole("admin");

            // For now, allow access in development
            return true;
        }
    }
}
