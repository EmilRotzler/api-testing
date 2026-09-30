using ApiTesting.Interfaces;
using ApiTesting.Logging;
using ApiTesting.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiTesting.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController(IWeatherForecastManager manager, ILogger<WeatherForecastController> logger)
    : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IEnumerable<WeatherForecast> Get()
    {
        logger.ForecastRequested();
        return manager.GetForecast();
    }

    [HttpPost("invalidate-cache")]
    public IActionResult InvalidateCache()
    {
        manager.InvalidateCache();
        logger.ForecastCacheInvalidated();
        return NoContent();
    }
}
