using ApiTesting.Core.Models;

namespace ApiTesting.Core.Interfaces;

public interface ILoginManager
{
    Task<LoginResult?> LoginAsync(string username, string password);
}
