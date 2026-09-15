using ApiTesting.Data;
using ApiTesting.Interfaces;
using ApiTesting.Managers;
using ApiTesting.Models;
using ApiTesting.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ApiTesting.Tests.Managers;

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

        var manager = new AuthManager(dbContext, hasher, CreateConfiguration());
        var result = await manager.LoginAsync("testuser", "s3cret!");

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.Token));
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_SetsLastLoginAtOnSuccess()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Argon2PasswordHasher();
        dbContext.Users.Add(CreateUser(hasher, "s3cret!"));
        await dbContext.SaveChangesAsync();

        var manager = new AuthManager(dbContext, hasher, CreateConfiguration());
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

        var manager = new AuthManager(dbContext, hasher, CreateConfiguration());
        var result = await manager.LoginAsync("testuser", "wrong-password");

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNullForInactiveUser()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Argon2PasswordHasher();
        dbContext.Users.Add(CreateUser(hasher, "s3cret!", isActive: false));
        await dbContext.SaveChangesAsync();

        var manager = new AuthManager(dbContext, hasher, CreateConfiguration());
        var result = await manager.LoginAsync("testuser", "s3cret!");

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNullForUnknownUsername()
    {
        await using var dbContext = CreateDbContext();
        var manager = new AuthManager(dbContext, new Argon2PasswordHasher(), CreateConfiguration());

        var result = await manager.LoginAsync("nobody", "whatever");

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_ReturnsTrueForValidToken()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Argon2PasswordHasher();
        dbContext.Users.Add(CreateUser(hasher, "s3cret!"));
        await dbContext.SaveChangesAsync();

        var manager = new AuthManager(dbContext, hasher, CreateConfiguration());
        var loginResult = await manager.LoginAsync("testuser", "s3cret!");

        var isValid = await manager.ValidateTokenAsync(loginResult!.Token);

        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateTokenAsync_ReturnsFalseForUnknownToken()
    {
        await using var dbContext = CreateDbContext();
        var manager = new AuthManager(dbContext, new Argon2PasswordHasher(), CreateConfiguration());

        var isValid = await manager.ValidateTokenAsync("not-a-real-token");

        Assert.False(isValid);
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

        var manager = new AuthManager(dbContext, hasher, CreateConfiguration());
        var isValid = await manager.ValidateTokenAsync("expired-token");

        Assert.False(isValid);
    }
}
