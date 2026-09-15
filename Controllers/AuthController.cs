using ApiTesting.Constants;
using ApiTesting.Dtos;
using ApiTesting.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ApiTesting.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IAuthManager authManager) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authManager.LoginAsync(request.Username, request.Password);

        if (result is null)
        {
            return Unauthorized(new { error = "Invalid username or password" });
        }

        Response.Cookies.Append(AuthConstants.TokenCookieName, result.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = new DateTimeOffset(result.ExpiresAt, TimeSpan.Zero),
        });

        return Ok(new LoginResponse(result.ExpiresAt));
    }
}
