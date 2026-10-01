using ApiTesting.Core.Interfaces;
using ApiTesting.Core.Models;
using ApiTesting.Infrastructure.Data;

namespace ApiTesting.Core.Managers;

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
