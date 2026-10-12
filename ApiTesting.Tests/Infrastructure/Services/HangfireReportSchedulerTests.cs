using ApiTesting.Core.Jobs;
using ApiTesting.Infrastructure.Services;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Xunit;

namespace ApiTesting.Tests.Infrastructure.Services;

public class HangfireReportSchedulerTests
{
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

    [Fact]
    public void QueueWeatherReport_EnqueuesWeatherReportJobAndReturnsJobId()
    {
        var jobClient = new FakeBackgroundJobClient();
        var scheduler = new HangfireReportScheduler(jobClient);

        var jobId = scheduler.QueueWeatherReport();

        Assert.Equal("job-42", jobId);
        var (job, state) = Assert.Single(jobClient.Created);
        Assert.Equal(typeof(WeatherReportJob), job.Type);
        Assert.Equal(nameof(WeatherReportJob.RunAsync), job.Method.Name);
        Assert.IsType<EnqueuedState>(state);
    }
}
