using System.Globalization;
using ApiTesting.Common.Configuration;
using ApiTesting.Common.Logging;
using ApiTesting.Core.Interfaces;
using ApiTesting.Core.Models;
using ApiTesting.Infrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ApiTesting.Core.Jobs;

[AutomaticRetry(Attempts = 3)]
public class WeatherReportJob(
    AppDbContext dbContext,
    IWeatherReportPdfGenerator pdfGenerator,
    IEmailSender emailSender,
    IOptions<EmailOptions> options,
    TimeProvider timeProvider,
    ILogger<WeatherReportJob> logger
)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        logger.ReportJobStarted();

        // Read straight from the database rather than the 30s forecast cache so the report is always current.
        var forecasts = await dbContext
            .WeatherForecasts.OrderBy(f => f.Date)
            .ThenBy(f => f.Location)
            .ToListAsync(cancellationToken);

        var generatedAt = timeProvider.GetUtcNow().UtcDateTime;
        var pdf = pdfGenerator.Generate(forecasts, generatedAt);
        var date = generatedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var time = generatedAt.ToString("HH:mm", CultureInfo.InvariantCulture);

        var message = new EmailMessage(
            options.Value.ReportRecipient,
            $"Weather forecast report {date}",
            $"Attached is the weather forecast report generated at {date} {time} UTC, covering {forecasts.Count} forecast(s).",
            [new EmailAttachment($"weather-report-{date}.pdf", "application/pdf", pdf)]
        );

        // No try/catch: failures propagate so Hangfire records them and retries.
        await emailSender.SendAsync(message, cancellationToken);

        logger.ReportSent(forecasts.Count, pdf.Length);
    }
}
