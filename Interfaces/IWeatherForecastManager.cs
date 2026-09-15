using ApiTesting.Models;

namespace ApiTesting.Interfaces;

public interface IWeatherForecastManager
{
    IEnumerable<WeatherForecast> GetForecast();

    void InvalidateCache();
}
