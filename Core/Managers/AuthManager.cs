using System.Security.Cryptography;
using ApiTesting.Api.Dtos;
using ApiTesting.Common.Logging;
using ApiTesting.Core.Interfaces;
using ApiTesting.Core.Models;
using ApiTesting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiTesting.Core.Managers;

public class AuthManager(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IConfiguration configuration,
    ILogger<AuthManager> logger)
    : IAuthManager
{
    public async Task<LoginResult?> LoginAsync(string username, string password)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Username == username);
        if (user is null)
        {
            logger.LoginFailedUnknownUser(username);
            return null;
        }

        if (!user.IsActive)
        {
            logger.LoginFailedInactiveUser(username, user.Id);
            return null;
        }

        if (!passwordHasher.VerifyPassword(user.PasswordHash, password))
        {
            logger.LoginFailedInvalidPassword(username, user.Id);
            return null;
        }

        var now = DateTime.UtcNow;
        var expiresAt = now.AddHours(configuration.GetValue("Auth:TokenLifetimeHours", 24));
        var token = GenerateToken();

        dbContext.AuthTokens.Add(new AuthToken
        {
            Token = token,
            UserId = user.Id,
            CreatedAt = now,
            ExpiresAt = expiresAt,
        });

        user.LastLoginAt = now;
        user.UpdatedAt = now;

        await dbContext.SaveChangesAsync();

        logger.LoginSucceeded(user.Id, expiresAt);
        return new LoginResult(token, expiresAt);
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        return await FindValidTokenAsync(token) is not null;
    }

    public async Task<User?> GetUserByTokenAsync(string token)
    {
        var authToken = await FindValidTokenAsync(token);
        if (authToken is null)
        {
            return null;
        }

        var user = await dbContext.Users.FindAsync(authToken.UserId);
        if (user is null || !user.IsActive)
        {
            logger.TokenUserInactive(authToken.UserId);
            return null;
        }

        return user;
    }

    private async Task<AuthToken?> FindValidTokenAsync(string token)
    {
        var authToken = await dbContext.AuthTokens.SingleOrDefaultAsync(t => t.Token == token);
        if (authToken is null)
        {
            logger.TokenNotFound();
            return null;
        }

        if (authToken.ExpiresAt <= DateTime.UtcNow)
        {
            logger.TokenExpired(authToken.UserId, authToken.ExpiresAt);
            return null;
        }

        return authToken;
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
