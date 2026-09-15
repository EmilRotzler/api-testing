using ApiTesting.Interfaces;
using ApiTesting.Models;
using Microsoft.AspNetCore.Mvc;

namespace ApiTesting.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController(IWeatherForecastManager manager) : ControllerBase
{
    [HttpGet]
    public IEnumerable<WeatherForecast> Get()
    {
        return manager.GetForecast();
    }

    [HttpPost("invalidate-cache")]
    public IActionResult InvalidateCache()
    {
        manager.InvalidateCache();
        return NoContent();
    }
}
