using ApiTesting.Core.Interfaces;
using ApiTesting.Core.Jobs;
using ApiTesting.Core.Managers;
using ApiTesting.Infrastructure.Data;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
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

    private sealed class FakeBackgroundJobClient : IBackgroundJobClient
    {
        public List<(Job Job, IState State)> Created { get; } = [];

        public string Create(Job job, IState state)
        {
            Created.Add((job, state));
            return "job-42";
        }

        public bool ChangeState(string jobId, IState state, string expectedState) =>
            throw new NotImplementedException();
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task QueueEmailReport_EnqueuesWeatherReportJobAndReturnsJobId()
    {
        await using var dbContext = CreateDbContext();
        var jobClient = new FakeBackgroundJobClient();
        var manager = new WeatherForecastManager(new FakeCacheService(), dbContext, jobClient);

        var jobId = manager.QueueEmailReport();

        Assert.Equal("job-42", jobId);
        var (job, state) = Assert.Single(jobClient.Created);
        Assert.Equal(typeof(WeatherReportJob), job.Type);
        Assert.Equal(nameof(WeatherReportJob.RunAsync), job.Method.Name);
        Assert.IsType<EnqueuedState>(state);
    }
}
