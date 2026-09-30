using ApiTesting.Constants;
using ApiTesting.Interfaces;
using ApiTesting.Logging;
using Microsoft.AspNetCore.Authorization;

namespace ApiTesting.Middleware;

public class TokenAuthMiddleware(IAuthManager authManager, ILogger<TokenAuthMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await next(context);
            return;
        }

        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (!context.Request.Cookies.TryGetValue(AuthConstants.TokenCookieName, out var token))
        {
            logger.RequestRejectedMissingToken(context.Request.Method, context.Request.Path, ipAddress);
            await WriteUnauthorized(context);
            return;
        }

        if (!await authManager.ValidateTokenAsync(token))
        {
            logger.RequestRejectedInvalidToken(context.Request.Method, context.Request.Path, ipAddress);
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
