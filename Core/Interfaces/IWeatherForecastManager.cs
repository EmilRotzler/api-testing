using ApiTesting.Core.Models;

namespace ApiTesting.Core.Interfaces;

public interface IWeatherForecastManager
{
    IEnumerable<WeatherForecast> GetForecast();

    void InvalidateCache();
}
