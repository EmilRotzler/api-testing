using ApiTesting.Common.Configuration;
using ApiTesting.Common.Logging;
using ApiTesting.Core.Interfaces;
using ApiTesting.Core.Managers;
using ApiTesting.Core.Models;
using ApiTesting.Infrastructure.Data;
using ApiTesting.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
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

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static readonly DateTimeOffset Now = new(2026, 3, 14, 9, 30, 0, TimeSpan.Zero);

    private static IOptions<AuthOptions> CreateOptions(int tokenLifetimeHours = 24)
    {
        return Options.Create(new AuthOptions { TokenLifetimeHours = tokenLifetimeHours });
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
        var manager = new AuthManager(dbContext, hasher, CreateOptions(), TimeProvider.System, logger);
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

        var manager = new AuthManager(dbContext, hasher, CreateOptions(), TimeProvider.System, new FakeLogger<AuthManager>());
        await manager.LoginAsync("testuser", "s3cret!");

        var updated = await dbContext.Users.SingleAsync(u => u.Username == "testuser");
        Assert.NotNull(updated.LastLoginAt);
    }

    [Fact]
    public async Task LoginAsync_ExpiresTokenAfterConfiguredLifetime()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Argon2PasswordHasher();
        dbContext.Users.Add(CreateUser(hasher, "s3cret!"));
        await dbContext.SaveChangesAsync();

        var manager = new AuthManager(
            dbContext,
            hasher,
            CreateOptions(tokenLifetimeHours: 2),
            new MutableTimeProvider(Now),
            new FakeLogger<AuthManager>()
        );
        var result = await manager.LoginAsync("testuser", "s3cret!");

        Assert.Equal(Now.UtcDateTime.AddHours(2), result!.ExpiresAt);
        var updated = await dbContext.Users.SingleAsync(u => u.Username == "testuser");
        Assert.Equal(Now.UtcDateTime, updated.LastLoginAt);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNullForWrongPassword()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Argon2PasswordHasher();
        dbContext.Users.Add(CreateUser(hasher, "s3cret!"));
        await dbContext.SaveChangesAsync();

        var logger = new FakeLogger<AuthManager>();
        var manager = new AuthManager(dbContext, hasher, CreateOptions(), TimeProvider.System, logger);
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
        var manager = new AuthManager(dbContext, hasher, CreateOptions(), TimeProvider.System, logger);
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
        var manager = new AuthManager(dbContext, new Argon2PasswordHasher(), CreateOptions(), TimeProvider.System, logger);

        var result = await manager.LoginAsync("nobody", "whatever");

        Assert.Null(result);

        var log = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, log.Level);
        Assert.Equal(LogEventIds.AuthManager.LoginFailedUnknownUser, log.Id.Id);
        Assert.Contains("nobody", log.Message);
        Assert.DoesNotContain("whatever", log.Message);
    }

    [Fact]
    public async Task GetUserByTokenAsync_ReturnsUserForValidToken()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Argon2PasswordHasher();
        var user = CreateUser(hasher, "s3cret!");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var manager = new AuthManager(dbContext, hasher, CreateOptions(), TimeProvider.System, new FakeLogger<AuthManager>());
        var loginResult = await manager.LoginAsync("testuser", "s3cret!");

        var result = await manager.GetUserByTokenAsync(loginResult!.Token);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result!.Id);
    }

    [Fact]
    public async Task GetUserByTokenAsync_ReturnsNullForUnknownToken()
    {
        await using var dbContext = CreateDbContext();
        var logger = new FakeLogger<AuthManager>();
        var manager = new AuthManager(dbContext, new Argon2PasswordHasher(), CreateOptions(), TimeProvider.System, logger);

        var result = await manager.GetUserByTokenAsync("not-a-real-token");

        Assert.Null(result);

        var log = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, log.Level);
        Assert.Equal(LogEventIds.AuthManager.TokenNotFound, log.Id.Id);
        Assert.DoesNotContain("not-a-real-token", log.Message);
    }

    [Fact]
    public async Task GetUserByTokenAsync_ReturnsNullForExpiredToken()
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
        var manager = new AuthManager(dbContext, hasher, CreateOptions(), TimeProvider.System, logger);

        var result = await manager.GetUserByTokenAsync("expired-token");

        Assert.Null(result);

        var log = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, log.Level);
        Assert.Equal(LogEventIds.AuthManager.TokenExpired, log.Id.Id);
        Assert.DoesNotContain("expired-token", log.Message);
    }

    [Fact]
    public async Task GetUserByTokenAsync_ReturnsNullOnceTokenLifetimeHasElapsed()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Argon2PasswordHasher();
        dbContext.Users.Add(CreateUser(hasher, "s3cret!"));
        await dbContext.SaveChangesAsync();

        var timeProvider = new MutableTimeProvider(Now);
        var logger = new FakeLogger<AuthManager>();
        var manager = new AuthManager(dbContext, hasher, CreateOptions(tokenLifetimeHours: 2), timeProvider, logger);
        var loginResult = await manager.LoginAsync("testuser", "s3cret!");

        timeProvider.Now = Now.AddHours(2).AddTicks(-1);
        Assert.NotNull(await manager.GetUserByTokenAsync(loginResult!.Token));

        timeProvider.Now = Now.AddHours(2);
        Assert.Null(await manager.GetUserByTokenAsync(loginResult.Token));

        var log = logger.Collector.LatestRecord;
        Assert.Equal(LogEventIds.AuthManager.TokenExpired, log.Id.Id);
    }

    [Fact]
    public async Task GetUserByTokenAsync_ReturnsNullForInactiveUser()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Argon2PasswordHasher();
        var user = CreateUser(hasher, "s3cret!");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var logger = new FakeLogger<AuthManager>();
        var manager = new AuthManager(dbContext, hasher, CreateOptions(), TimeProvider.System, logger);
        var loginResult = await manager.LoginAsync("testuser", "s3cret!");

        user.IsActive = false;
        await dbContext.SaveChangesAsync();

        var result = await manager.GetUserByTokenAsync(loginResult!.Token);

        Assert.Null(result);

        var log = logger.Collector.LatestRecord;
        Assert.Equal(LogLevel.Information, log.Level);
        Assert.Equal(LogEventIds.AuthManager.TokenUserInactive, log.Id.Id);
        Assert.DoesNotContain(loginResult.Token, log.Message);
    }
}
