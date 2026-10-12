using ApiTesting.Core.Interfaces;
using ApiTesting.Core.Jobs;
using Hangfire;

namespace ApiTesting.Infrastructure.Services;

public class HangfireReportScheduler(IBackgroundJobClient backgroundJobClient) : IReportScheduler
{
    public string QueueWeatherReport()
    {
        // Hangfire swaps CancellationToken.None for its own shutdown token when the job runs.
        return backgroundJobClient.Enqueue<WeatherReportJob>(job => job.RunAsync(CancellationToken.None));
    }
}
