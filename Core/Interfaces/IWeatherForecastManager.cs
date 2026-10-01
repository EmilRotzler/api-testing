using ApiTesting.Core.Models;

namespace ApiTesting.Core.Interfaces;

public interface IWeatherForecastManager
{
    IEnumerable<WeatherForecast> GetForecast();

    void InvalidateCache();

    /// Queues a background job that emails the forecast report; returns the job id.
    string QueueEmailReport();
}
