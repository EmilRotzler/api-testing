using ApiTesting.Data;
using ApiTesting.Interfaces;
using ApiTesting.Models;

namespace ApiTesting.Managers;

public class WeatherForecastManager(ICacheService cache, AppDbContext dbContext) : IWeatherForecastManager
{
    private const string ForecastCacheKey = "weatherforecast";
    private static readonly TimeSpan ForecastCacheTtl = TimeSpan.FromSeconds(30);

    public IEnumerable<WeatherForecast> GetForecast()
    {
        return cache.GetOrCreate(ForecastCacheKey, ForecastCacheTtl, () =>
            dbContext.WeatherForecasts.ToList());
    }

    public void InvalidateCache()
    {
        cache.Invalidate(ForecastCacheKey);
    }
}
