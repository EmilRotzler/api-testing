using ApiTesting.Common.Constants;
using ApiTesting.Common.Logging;
using ApiTesting.Core.Interfaces;
using Hangfire;
using Hangfire.Dashboard;

namespace ApiTesting.Api.Filters;

/// Lets only admins into the Hangfire dashboard. TokenAuthMiddleware has already rejected requests
/// without a valid cookie; this adds the role check. Hangfire answers a denial with 401 because the app
/// doesn't populate HttpContext.User.
public class AdminDashboardAuthorizationFilter : IDashboardAsyncAuthorizationFilter
{
    public Task<bool> AuthorizeAsync(DashboardContext context) =>
        IsAuthorizedAsync(context.GetHttpContext());

    public async Task<bool> IsAuthorizedAsync(HttpContext httpContext)
    {
        // The filter is constructed once at startup, so per-request services come from RequestServices.
        var logger = httpContext.RequestServices.GetRequiredService<
            ILogger<AdminDashboardAuthorizationFilter>
        >();

        if (!httpContext.Request.Cookies.TryGetValue(AuthConstants.TokenCookieName, out var token))
        {
            logger.DashboardAccessDenied("MissingToken");
            return false;
        }

        var authManager = httpContext.RequestServices.GetRequiredService<IAuthManager>();
        var user = await authManager.GetUserByTokenAsync(token);
        if (user is null)
        {
            logger.DashboardAccessDenied("InvalidToken");
            return false;
        }

        if (user.Role != AuthConstants.AdminRole)
        {
            logger.DashboardAccessDenied("NotAdmin");
            return false;
        }

        return true;
    }
}
