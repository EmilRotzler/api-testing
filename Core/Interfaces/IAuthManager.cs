using ApiTesting.Api.Dtos;

namespace ApiTesting.Core.Interfaces;

public interface IAuthManager
{
    Task<LoginResult?> LoginAsync(string username, string password);

    Task<bool> ValidateTokenAsync(string token);
}
