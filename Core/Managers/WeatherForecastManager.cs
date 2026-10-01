using ApiTesting.Core.Interfaces;
using ApiTesting.Core.Jobs;
using ApiTesting.Core.Models;
using ApiTesting.Infrastructure.Data;
using Hangfire;

namespace ApiTesting.Core.Managers;

public class WeatherForecastManager(ICacheService cache, AppDbContext dbContext, IBackgroundJobClient backgroundJobClient)
    : IWeatherForecastManager
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

    public string QueueEmailReport()
    {
        // Hangfire swaps CancellationToken.None for its own shutdown token when the job runs.
        return backgroundJobClient.Enqueue<WeatherReportJob>(job => job.RunAsync(CancellationToken.None));
    }
}
