using ApiTesting.Controllers;
using ApiTesting.Interfaces;
using ApiTesting.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ApiTesting.Tests.Controllers;

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
        var controller = new WeatherForecastController(manager);

        var result = controller.Get();

        Assert.Same(expected, result);
    }

    [Fact]
    public void InvalidateCache_CallsManagerInvalidateCache()
    {
        var manager = new FakeWeatherForecastManager();
        var controller = new WeatherForecastController(manager);

        controller.InvalidateCache();

        Assert.Equal(1, manager.InvalidateCacheCallCount);
    }

    [Fact]
    public void InvalidateCache_ReturnsNoContent()
    {
        var manager = new FakeWeatherForecastManager();
        var controller = new WeatherForecastController(manager);

        var result = controller.InvalidateCache();

        Assert.IsType<NoContentResult>(result);
    }
}
