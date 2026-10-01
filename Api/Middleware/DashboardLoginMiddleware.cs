using ApiTesting.Api.Filters;

namespace ApiTesting.Api.Middleware;

/// Shows the static login page at /hangfire to anyone who isn't allowed into the dashboard, instead of
/// a bare 401. Rewrites the path (the URL stays /hangfire), so it must run before UseStaticFiles.
public class DashboardLoginMiddleware : IMiddleware
{
    public const string DashboardPath = "/hangfire";
    public const string LoginPagePath = "/hangfire/login.html";

    private readonly AdminDashboardAuthorizationFilter dashboardFilter = new();

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (IsDashboardRoot(context.Request) && !await dashboardFilter.IsAuthorizedAsync(context))
        {
            context.Request.Path = LoginPagePath;
        }

        await next(context);
    }

    private static bool IsDashboardRoot(HttpRequest request) =>
        HttpMethods.IsGet(request.Method)
        && string.Equals(request.Path.Value?.TrimEnd('/'), DashboardPath, StringComparison.OrdinalIgnoreCase);
}
