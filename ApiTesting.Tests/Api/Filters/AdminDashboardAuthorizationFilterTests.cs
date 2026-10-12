using ApiTesting.Api.Filters;
using ApiTesting.Common.Constants;
using ApiTesting.Common.Logging;
using ApiTesting.Core.Interfaces;
using ApiTesting.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace ApiTesting.Tests.Api.Filters;

public class AdminDashboardAuthorizationFilterTests
{
    private sealed class FakeTokenValidator : ITokenValidator
    {
        public Dictionary<string, User> UsersByToken { get; } = [];

        public Task<User?> GetUserByTokenAsync(string token) =>
            Task.FromResult(UsersByToken.GetValueOrDefault(token));
    }

    private static DefaultHttpContext CreateContext(
        FakeTokenValidator tokenValidator,
        FakeLogger<AdminDashboardAuthorizationFilter> logger,
        string? cookieToken
    )
    {
        var services = new ServiceCollection()
            .AddSingleton<ITokenValidator>(tokenValidator)
            .AddSingleton<ILogger<AdminDashboardAuthorizationFilter>>(logger)
            .BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = services };
        if (cookieToken is not null)
        {
            context.Request.Headers.Append(
                "Cookie",
                $"{AuthConstants.TokenCookieName}={cookieToken}"
            );
        }

        return context;
    }

    private static User CreateUser(string role) =>
        new()
        {
            Id = 7,
            Username = "someone",
            Role = role,
        };

    [Fact]
    public async Task IsAuthorizedAsync_AllowsAdmin()
    {
        var tokenValidator = new FakeTokenValidator();
        tokenValidator.UsersByToken["admin-token"] = CreateUser(AuthConstants.AdminRole);
        var logger = new FakeLogger<AdminDashboardAuthorizationFilter>();

        var allowed = await new AdminDashboardAuthorizationFilter().IsAuthorizedAsync(
            CreateContext(tokenValidator, logger, "admin-token")
        );

        Assert.True(allowed);
        Assert.Empty(logger.Collector.GetSnapshot());
    }

    [Fact]
    public async Task IsAuthorizedAsync_DeniesNonAdmin()
    {
        var tokenValidator = new FakeTokenValidator();
        tokenValidator.UsersByToken["user-token"] = CreateUser("User");
        var logger = new FakeLogger<AdminDashboardAuthorizationFilter>();

        var allowed = await new AdminDashboardAuthorizationFilter().IsAuthorizedAsync(
            CreateContext(tokenValidator, logger, "user-token")
        );

        Assert.False(allowed);
        var log = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, log.Level);
        Assert.Equal(
            LogEventIds.AdminDashboardAuthorizationFilter.DashboardAccessDenied,
            log.Id.Id
        );
        Assert.Contains("NotAdmin", log.Message);
        Assert.DoesNotContain("user-token", log.Message);
    }

    [Fact]
    public async Task IsAuthorizedAsync_DeniesMissingCookie()
    {
        var logger = new FakeLogger<AdminDashboardAuthorizationFilter>();

        var allowed = await new AdminDashboardAuthorizationFilter().IsAuthorizedAsync(
            CreateContext(new FakeTokenValidator(), logger, cookieToken: null)
        );

        Assert.False(allowed);
        var log = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(
            LogEventIds.AdminDashboardAuthorizationFilter.DashboardAccessDenied,
            log.Id.Id
        );
        Assert.Contains("MissingToken", log.Message);
    }

    [Fact]
    public async Task IsAuthorizedAsync_DeniesInvalidToken()
    {
        var logger = new FakeLogger<AdminDashboardAuthorizationFilter>();

        var allowed = await new AdminDashboardAuthorizationFilter().IsAuthorizedAsync(
            CreateContext(new FakeTokenValidator(), logger, "bad-token")
        );

        Assert.False(allowed);
        var log = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(
            LogEventIds.AdminDashboardAuthorizationFilter.DashboardAccessDenied,
            log.Id.Id
        );
        Assert.Contains("InvalidToken", log.Message);
        Assert.DoesNotContain("bad-token", log.Message);
    }
}
