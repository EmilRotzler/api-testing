using ApiTesting.Interfaces;
using ApiTesting.Models;

namespace ApiTesting.Managers;

public class WeatherForecastManager(ICacheService cache) : IWeatherForecastManager
{
    private const string ForecastCacheKey = "weatherforecast";
    private static readonly TimeSpan ForecastCacheTtl = TimeSpan.FromSeconds(30);

    private static readonly string[] Summaries =
    [
        "Freezing",
        "Bracing",
        "Chilly",
        "Cool",
        "Mild",
        "Warm",
        "Balmy",
        "Hot",
        "Sweltering",
        "Scorching",
    ];

    public IEnumerable<WeatherForecast> GetForecast()
    {
        return cache.GetOrCreate(ForecastCacheKey, ForecastCacheTtl, () =>
            Enumerable
                .Range(1, 5)
                .Select(index => new WeatherForecast(
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    Summaries[Random.Shared.Next(Summaries.Length)]
                ))
                .ToList());
    }

    public void InvalidateCache()
    {
        cache.Invalidate(ForecastCacheKey);
    }
}
