namespace ApiTesting.Core.Interfaces;

public interface IReportScheduler
{
    /// Queues a background job that emails the forecast report; returns the job id.
    string QueueWeatherReport();
}
