using ApiTesting.Core.Interfaces;
using ApiTesting.Core.Managers;
using ApiTesting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ApiTesting.Tests.Core.Managers;

public class WeatherForecastManagerTests
{
    private sealed class FakeCacheService : ICacheService
    {
        public T GetOrCreate<T>(string key, TimeSpan ttl, Func<T> factory) => factory();

        public void Invalidate(string key) { }
    }

    private sealed class FakeReportScheduler : IReportScheduler
    {
        public int QueuedCount { get; private set; }

        public string QueueWeatherReport()
        {
            QueuedCount++;
            return "job-42";
        }
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task QueueEmailReport_QueuesWeatherReportAndReturnsJobId()
    {
        await using var dbContext = CreateDbContext();
        var scheduler = new FakeReportScheduler();
        var manager = new WeatherForecastManager(new FakeCacheService(), dbContext, scheduler);

        var jobId = manager.QueueEmailReport();

        Assert.Equal("job-42", jobId);
        Assert.Equal(1, scheduler.QueuedCount);
    }
}
