using ApiTesting.Dtos;

namespace ApiTesting.Interfaces;

public interface IAuthManager
{
    Task<LoginResult?> LoginAsync(string username, string password);

    Task<bool> ValidateTokenAsync(string token);
}
