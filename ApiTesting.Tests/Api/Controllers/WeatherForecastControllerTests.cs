using ApiTesting.Api.Controllers;
using ApiTesting.Common.Logging;
using ApiTesting.Core.Interfaces;
using ApiTesting.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace ApiTesting.Tests.Api.Controllers;

public class WeatherForecastControllerTests
{
    private sealed class FakeWeatherForecastManager : IWeatherForecastManager
    {
        public IEnumerable<WeatherForecast> Forecasts { get; set; } = [];

        public int InvalidateCacheCallCount { get; private set; }

        public IEnumerable<WeatherForecast> GetForecast() => Forecasts;

        public void InvalidateCache() => InvalidateCacheCallCount++;
    }

    [Fact]
    public void Get_ReturnsForecastsFromManager()
    {
        var expected = new[]
        {
            new WeatherForecast(new DateOnly(2026, 9, 30), 20, "Mild"),
        };
        var manager = new FakeWeatherForecastManager { Forecasts = expected };
        var controller = new WeatherForecastController(manager, new FakeLogger<WeatherForecastController>());

        var result = controller.Get();

        Assert.Same(expected, result);
    }

    [Fact]
    public void InvalidateCache_CallsManagerInvalidateCache()
    {
        var manager = new FakeWeatherForecastManager();
        var logger = new FakeLogger<WeatherForecastController>();
        var controller = new WeatherForecastController(manager, logger);

        controller.InvalidateCache();

        Assert.Equal(1, manager.InvalidateCacheCallCount);

        var log = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, log.Level);
        Assert.Equal(LogEventIds.WeatherForecastController.ForecastCacheInvalidated, log.Id.Id);
    }

    [Fact]
    public void InvalidateCache_ReturnsNoContent()
    {
        var manager = new FakeWeatherForecastManager();
        var controller = new WeatherForecastController(manager, new FakeLogger<WeatherForecastController>());

        var result = controller.InvalidateCache();

        Assert.IsType<NoContentResult>(result);
    }
}
