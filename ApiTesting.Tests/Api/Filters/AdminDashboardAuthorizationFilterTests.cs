using ApiTesting.Api.Dtos;
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
    private sealed class FakeAuthManager : IAuthManager
    {
        public Dictionary<string, User> UsersByToken { get; } = [];

        public Task<LoginResult?> LoginAsync(string username, string password) =>
            throw new NotImplementedException();

        public Task<bool> ValidateTokenAsync(string token) => throw new NotImplementedException();

        public Task<User?> GetUserByTokenAsync(string token) =>
            Task.FromResult(UsersByToken.GetValueOrDefault(token));
    }

    private static DefaultHttpContext CreateContext(
        FakeAuthManager authManager,
        FakeLogger<AdminDashboardAuthorizationFilter> logger,
        string? cookieToken
    )
    {
        var services = new ServiceCollection()
            .AddSingleton<IAuthManager>(authManager)
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
        var authManager = new FakeAuthManager();
        authManager.UsersByToken["admin-token"] = CreateUser(AuthConstants.AdminRole);
        var logger = new FakeLogger<AdminDashboardAuthorizationFilter>();

        var allowed = await new AdminDashboardAuthorizationFilter().IsAuthorizedAsync(
            CreateContext(authManager, logger, "admin-token")
        );

        Assert.True(allowed);
        Assert.Empty(logger.Collector.GetSnapshot());
    }

    [Fact]
    public async Task IsAuthorizedAsync_DeniesNonAdmin()
    {
        var authManager = new FakeAuthManager();
        authManager.UsersByToken["user-token"] = CreateUser("User");
        var logger = new FakeLogger<AdminDashboardAuthorizationFilter>();

        var allowed = await new AdminDashboardAuthorizationFilter().IsAuthorizedAsync(
            CreateContext(authManager, logger, "user-token")
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
            CreateContext(new FakeAuthManager(), logger, cookieToken: null)
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
            CreateContext(new FakeAuthManager(), logger, "bad-token")
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
