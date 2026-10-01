using ApiTesting.Api.Controllers;
using ApiTesting.Api.Dtos;
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

        public string JobIdToReturn { get; set; } = "job-1";

        public int QueueEmailReportCallCount { get; private set; }

        public string QueueEmailReport()
        {
            QueueEmailReportCallCount++;
            return JobIdToReturn;
        }
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

    [Fact]
    public void EmailReport_QueuesReportAndReturnsAcceptedWithJobId()
    {
        var manager = new FakeWeatherForecastManager { JobIdToReturn = "job-42" };
        var logger = new FakeLogger<WeatherForecastController>();
        var controller = new WeatherForecastController(manager, logger);

        var result = controller.EmailReport();

        Assert.Equal(1, manager.QueueEmailReportCallCount);
        var accepted = Assert.IsType<AcceptedResult>(result);
        var body = Assert.IsType<EmailReportQueuedResponse>(accepted.Value);
        Assert.Equal("job-42", body.JobId);

        var log = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, log.Level);
        Assert.Equal(LogEventIds.WeatherForecastController.EmailReportQueued, log.Id.Id);
        Assert.Contains("job-42", log.Message);
    }
}
