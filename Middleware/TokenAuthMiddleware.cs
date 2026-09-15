using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace ApiTesting.Middleware;

public class TokenAuthMiddleware(IConfiguration configuration) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await next(context);
            return;
        }

        var expectedToken = configuration["Auth:Token"]!;

        var authHeader = context.Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";

        if (!authHeader.StartsWith(prefix, StringComparison.Ordinal))
        {
            await WriteUnauthorized(context);
            return;
        }

        var providedToken = authHeader[prefix.Length..];

        if (!TokensMatch(providedToken, expectedToken))
        {
            await WriteUnauthorized(context);
            return;
        }

        await next(context);
    }

    private static bool TokensMatch(string provided, string expected)
    {
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);

        return providedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }

    private static async Task WriteUnauthorized(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("""{"error": "Invalid or missing token"}""");
    }
}
