using System.Security.Cryptography;
using ApiTesting.Data;
using ApiTesting.Dtos;
using ApiTesting.Interfaces;
using ApiTesting.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiTesting.Managers;

public class AuthManager(AppDbContext dbContext, IPasswordHasher passwordHasher, IConfiguration configuration)
    : IAuthManager
{
    public async Task<LoginResult?> LoginAsync(string username, string password)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Username == username);
        if (user is null || !user.IsActive || !passwordHasher.VerifyPassword(user.PasswordHash, password))
        {
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

        return new LoginResult(token, expiresAt);
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        var authToken = await dbContext.AuthTokens.SingleOrDefaultAsync(t => t.Token == token);
        return authToken is not null && authToken.ExpiresAt > DateTime.UtcNow;
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
