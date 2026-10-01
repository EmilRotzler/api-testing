using ApiTesting.Api.Dtos;
using ApiTesting.Common.Logging;
using ApiTesting.Core.Interfaces;
using ApiTesting.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiTesting.Api.Controllers;

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

    [HttpPost("email-report")]
    public IActionResult EmailReport()
    {
        var jobId = manager.QueueEmailReport();
        logger.EmailReportQueued(jobId);
        return Accepted(new EmailReportQueuedResponse(jobId));
    }
}
