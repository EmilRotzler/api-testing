using ApiTesting.Api.Middleware;
using ApiTesting.Common.Constants;
using ApiTesting.Common.Logging;
using ApiTesting.Core.Interfaces;
using ApiTesting.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace ApiTesting.Tests.Api.Middleware;

public class TokenAuthMiddlewareTests
{
    private sealed class FakeTokenValidator : ITokenValidator
    {
        public User? User { get; set; }

        public Task<User?> GetUserByTokenAsync(string token) => Task.FromResult(User);
    }

    private static DefaultHttpContext CreateContext(bool allowAnonymous, string? cookieToken = null)
    {
        var context = new DefaultHttpContext();

        var metadata = allowAnonymous
            ? new EndpointMetadataCollection(new AllowAnonymousAttribute())
            : EndpointMetadataCollection.Empty;
        context.SetEndpoint(new Endpoint(null, metadata, "test"));

        if (cookieToken is not null)
        {
            context.Request.Headers.Append("Cookie", $"{AuthConstants.TokenCookieName}={cookieToken}");
        }

        return context;
    }

    [Fact]
    public async Task InvokeAsync_CallsNextForAllowAnonymousEndpoint()
    {
        var middleware = new TokenAuthMiddleware(new FakeTokenValidator(),new FakeLogger<TokenAuthMiddleware>());
        var context = CreateContext(allowAnonymous: true);
        var nextCalled = false;

        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsUnauthorizedWhenCookieMissing()
    {
        var logger = new FakeLogger<TokenAuthMiddleware>();
        var middleware = new TokenAuthMiddleware(new FakeTokenValidator(),logger);
        var context = CreateContext(allowAnonymous: false);
        context.Request.Method = "POST";
        context.Request.Path = "/weatherforecast/invalidate-cache";
        var nextCalled = false;

        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);

        var log = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, log.Level);
        Assert.Equal(LogEventIds.TokenAuthMiddleware.RequestRejectedMissingToken, log.Id.Id);
        Assert.Contains("/weatherforecast/invalidate-cache", log.Message);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsUnauthorizedWhenTokenInvalid()
    {
        var tokenValidator = new FakeTokenValidator { User = null };
        var logger = new FakeLogger<TokenAuthMiddleware>();
        var middleware = new TokenAuthMiddleware(tokenValidator, logger);
        var context = CreateContext(allowAnonymous: false, cookieToken: "bad-token");
        var nextCalled = false;

        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);

        var log = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, log.Level);
        Assert.Equal(LogEventIds.TokenAuthMiddleware.RequestRejectedInvalidToken, log.Id.Id);
        Assert.DoesNotContain("bad-token", log.Message);
    }

    [Fact]
    public async Task InvokeAsync_CallsNextWhenTokenValid()
    {
        var tokenValidator = new FakeTokenValidator { User = new User { Id = 1, Username = "someone" } };
        var logger = new FakeLogger<TokenAuthMiddleware>();
        var middleware = new TokenAuthMiddleware(tokenValidator, logger);
        var context = CreateContext(allowAnonymous: false, cookieToken: "good-token");
        var nextCalled = false;

        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Empty(logger.Collector.GetSnapshot());
    }
}
