using ApiTesting.Api.Dtos;
using ApiTesting.Core.Models;

namespace ApiTesting.Core.Interfaces;

public interface IAuthManager
{
    Task<LoginResult?> LoginAsync(string username, string password);

    Task<bool> ValidateTokenAsync(string token);

    Task<User?> GetUserByTokenAsync(string token);
}
