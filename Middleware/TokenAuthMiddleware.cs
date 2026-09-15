using ApiTesting.Constants;
using ApiTesting.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace ApiTesting.Middleware;

public class TokenAuthMiddleware(IAuthManager authManager) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await next(context);
            return;
        }

        if (!context.Request.Cookies.TryGetValue(AuthConstants.TokenCookieName, out var token) ||
            !await authManager.ValidateTokenAsync(token))
        {
            await WriteUnauthorized(context);
            return;
        }

        await next(context);
    }

    private static async Task WriteUnauthorized(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("""{"error": "Invalid or missing token"}""");
    }
}
