using ApiTesting.Core.Models;

namespace ApiTesting.Core.Interfaces;

public interface ITokenValidator
{
    /// Returns the token's user, or null when the token is unknown or expired or the user is inactive.
    Task<User?> GetUserByTokenAsync(string token);
}
