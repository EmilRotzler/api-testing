using ApiTesting.Api.Dtos;
using ApiTesting.Common.Constants;
using ApiTesting.Common.Logging;
using ApiTesting.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ApiTesting.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IAuthManager authManager, ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await authManager.LoginAsync(request.Username, request.Password);

        if (result is null)
        {
            logger.LoginRequestRejected(request.Username, ipAddress);
            return Unauthorized(new { error = "Invalid username or password" });
        }

        Response.Cookies.Append(AuthConstants.TokenCookieName, result.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = new DateTimeOffset(result.ExpiresAt, TimeSpan.Zero),
        });

        logger.LoginRequestSucceeded(request.Username, ipAddress);
        return Ok(new LoginResponse(result.ExpiresAt));
    }
}
