using ApiTesting.Common.Logging;
using ApiTesting.Core.Interfaces;
using ApiTesting.Core.Managers;
using ApiTesting.Core.Models;
using ApiTesting.Infrastructure.Data;
using ApiTesting.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace ApiTesting.Tests.Core.Managers;

public class AuthManagerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static IConfiguration CreateConfiguration(int tokenLifetimeHours = 24)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:TokenLifetimeHours"] = tokenLifetimeHours.ToString(),
            })
            .Build();
    }

    private static User CreateUser(IPasswordHasher hasher, string password, bool isActive = true)
    {
        return new User
        {
            Username = "testuser",
            Email = "testuser@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = hasher.HashPassword(password),
            Role = "User",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokenForValidCredentials()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Argon2PasswordHasher();
        dbContext.Users.Add(CreateUser(hasher, "s3cret!"));
        await dbContext.SaveChangesAsync();

        var logger = new FakeLogger<AuthManager>();
        var manager = new AuthManager(dbContext, hasher, CreateConfiguration(), logger);
        var result = await manager.LoginAsync("testuser", "s3cret!");

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.Token));
        Assert.True(result.ExpiresAt > DateTime.UtcNow);

        var log = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, log.Level);
        Assert.Equal(LogEventIds.AuthManager.LoginSucceeded, log.Id.Id);
        Assert.DoesNotContain(result.Token, log.Message);
        Assert.DoesNotContain("s3cret!", log.Message);
    }

    [Fact]
    public async Task LoginAsync_SetsLastLoginAtOnSuccess()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Argon2PasswordHasher();
        dbContext.Users.Add(CreateUser(hasher, "s3cret!"));
        await dbContext.SaveChangesAsync();

        var manager = new AuthManager(dbContext, hasher, CreateConfiguration(), new FakeLogger<AuthManager>());
        await manager.LoginAsync("testuser", "s3cret!");

        var updated = await dbContext.Users.SingleAsync(u => u.Username == "testuser");
        Assert.NotNull(updated.LastLoginAt);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNullForWrongPassword()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Argon2PasswordHasher();
        dbContext.Users.Add(CreateUser(hasher, "s3cret!"));
        await dbContext.SaveChangesAsync();

        var logger = new FakeLogger<AuthManager>();
        var manager = new AuthManager(dbContext, hasher, CreateConfiguration(), logger);
        var result = await manager.LoginAsync("testuser", "wrong-password");

        Assert.Null(result);

        var log = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, log.Level);
        Assert.Equal(LogEventIds.AuthManager.LoginFailedInvalidPassword, log.Id.Id);
        Assert.DoesNotContain("wrong-password", log.Message);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNullForInactiveUser()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Argon2PasswordHasher();
        dbContext.Users.Add(CreateUser(hasher, "s3cret!", isActive: false));
        await dbContext.SaveChangesAsync();

        var logger = new FakeLogger<AuthManager>();
        var manager = new AuthManager(dbContext, hasher, CreateConfiguration(), logger);
        var result = await manager.LoginAsync("testuser", "s3cret!");

        Assert.Null(result);

        var log = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, log.Level);
        Assert.Equal(LogEventIds.AuthManager.LoginFailedInactiveUser, log.Id.Id);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNullForUnknownUsername()
    {
        await using var dbContext = CreateDbContext();
        var logger = new FakeLogger<AuthManager>();
        var manager = new AuthManager(dbContext, new Argon2PasswordHasher(), CreateConfiguration(), logger);

        var result = await manager.LoginAsync("nobody", "whatever");

        Assert.Null(result);

        var log = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, log.Level);
        Assert.Equal(LogEventIds.AuthManager.LoginFailedUnknownUser, log.Id.Id);
        Assert.Contains("nobody", log.Message);
        Assert.DoesNotContain("whatever", log.Message);
    }

    [Fact]
    public async Task ValidateTokenAsync_ReturnsTrueForValidToken()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Argon2PasswordHasher();
        dbContext.Users.Add(CreateUser(hasher, "s3cret!"));
        await dbContext.SaveChangesAsync();

        var manager = new AuthManager(dbContext, hasher, CreateConfiguration(), new FakeLogger<AuthManager>());
        var loginResult = await manager.LoginAsync("testuser", "s3cret!");

        var isValid = await manager.ValidateTokenAsync(loginResult!.Token);

        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateTokenAsync_ReturnsFalseForUnknownToken()
    {
        await using var dbContext = CreateDbContext();
        var logger = new FakeLogger<AuthManager>();
        var manager = new AuthManager(dbContext, new Argon2PasswordHasher(), CreateConfiguration(), logger);

        var isValid = await manager.ValidateTokenAsync("not-a-real-token");

        Assert.False(isValid);

        var log = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, log.Level);
        Assert.Equal(LogEventIds.AuthManager.TokenNotFound, log.Id.Id);
        Assert.DoesNotContain("not-a-real-token", log.Message);
    }

    [Fact]
    public async Task ValidateTokenAsync_ReturnsFalseForExpiredToken()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Argon2PasswordHasher();
        var user = CreateUser(hasher, "s3cret!");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        dbContext.AuthTokens.Add(new AuthToken
        {
            Token = "expired-token",
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow.AddHours(-25),
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
        });
        await dbContext.SaveChangesAsync();

        var logger = new FakeLogger<AuthManager>();
        var manager = new AuthManager(dbContext, hasher, CreateConfiguration(), logger);
        var isValid = await manager.ValidateTokenAsync("expired-token");

        Assert.False(isValid);

        var log = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, log.Level);
        Assert.Equal(LogEventIds.AuthManager.TokenExpired, log.Id.Id);
        Assert.DoesNotContain("expired-token", log.Message);
    }
}
