using ApiTesting.Common.Configuration;
using ApiTesting.Common.Logging;
using ApiTesting.Core.Interfaces;
using ApiTesting.Core.Jobs;
using ApiTesting.Core.Models;
using ApiTesting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Xunit;

namespace ApiTesting.Tests.Core.Jobs;

public class WeatherReportJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 8, 30, 0, TimeSpan.Zero);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakePdfGenerator : IWeatherReportPdfGenerator
    {
        public byte[] Output { get; } = [0x25, 0x50, 0x44, 0x46];

        public IReadOnlyList<WeatherForecast>? ReceivedForecasts { get; private set; }

        public DateTime? ReceivedGeneratedAt { get; private set; }

        public byte[] Generate(IReadOnlyList<WeatherForecast> forecasts, DateTime generatedAtUtc)
        {
            ReceivedForecasts = forecasts;
            ReceivedGeneratedAt = generatedAtUtc;
            return Output;
        }
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = [];

        public Exception? ExceptionToThrow { get; set; }

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static IOptions<EmailOptions> CreateOptions() =>
        Options.Create(
            new EmailOptions
            {
                From = "weather@localhost",
                ReportRecipient = "reports@example.com",
                Smtp = new SmtpOptions { Host = "localhost", Port = 1025 },
            }
        );

    private static WeatherReportJob CreateJob(
        AppDbContext dbContext,
        FakePdfGenerator pdfGenerator,
        FakeEmailSender emailSender,
        FakeLogger<WeatherReportJob> logger
    ) =>
        new(
            dbContext,
            pdfGenerator,
            emailSender,
            CreateOptions(),
            new FixedTimeProvider(Now),
            logger
        );

    [Fact]
    public async Task RunAsync_EmailsPdfOfForecastsToConfiguredRecipient()
    {
        await using var dbContext = CreateDbContext();
        dbContext.WeatherForecasts.AddRange(
            new WeatherForecast(new DateOnly(2026, 10, 3), 15, "Mild") { Location = "Oslo" },
            new WeatherForecast(new DateOnly(2026, 10, 2), 12, "Cool") { Location = "Oslo" },
            new WeatherForecast(new DateOnly(2026, 10, 2), 10, "Cool") { Location = "Bergen" }
        );
        await dbContext.SaveChangesAsync();

        var pdfGenerator = new FakePdfGenerator();
        var emailSender = new FakeEmailSender();
        var logger = new FakeLogger<WeatherReportJob>();

        await CreateJob(dbContext, pdfGenerator, emailSender, logger)
            .RunAsync(CancellationToken.None);

        Assert.NotNull(pdfGenerator.ReceivedForecasts);
        Assert.Equal(
            new[]
            {
                ("Bergen", new DateOnly(2026, 10, 2)),
                ("Oslo", new DateOnly(2026, 10, 2)),
                ("Oslo", new DateOnly(2026, 10, 3)),
            },
            pdfGenerator.ReceivedForecasts!.Select(f => (f.Location, f.Date))
        );
        Assert.Equal(Now.UtcDateTime, pdfGenerator.ReceivedGeneratedAt);

        var message = Assert.Single(emailSender.Sent);
        Assert.Equal("reports@example.com", message.To);
        Assert.Equal("Weather forecast report 2026-10-01", message.Subject);
        var attachment = Assert.Single(message.Attachments);
        Assert.Equal("weather-report-2026-10-01.pdf", attachment.FileName);
        Assert.Equal("application/pdf", attachment.ContentType);
        Assert.Same(pdfGenerator.Output, attachment.Content);

        var logs = logger.Collector.GetSnapshot();
        Assert.Equal(
            new[]
            {
                LogEventIds.WeatherReportJob.ReportJobStarted,
                LogEventIds.WeatherReportJob.ReportSent,
            },
            logs.Select(l => l.Id.Id)
        );
        Assert.All(logs, l => Assert.Equal(LogLevel.Information, l.Level));
        Assert.All(logs, l => Assert.DoesNotContain("reports@example.com", l.Message));
    }

    [Fact]
    public async Task RunAsync_SendsReportWhenNoForecasts()
    {
        await using var dbContext = CreateDbContext();
        var pdfGenerator = new FakePdfGenerator();
        var emailSender = new FakeEmailSender();

        await CreateJob(dbContext, pdfGenerator, emailSender, new FakeLogger<WeatherReportJob>())
            .RunAsync(CancellationToken.None);

        Assert.Empty(pdfGenerator.ReceivedForecasts!);
        Assert.Single(emailSender.Sent);
    }

    [Fact]
    public async Task RunAsync_PropagatesSendFailureSoHangfireRetries()
    {
        await using var dbContext = CreateDbContext();
        var emailSender = new FakeEmailSender
        {
            ExceptionToThrow = new InvalidOperationException("smtp down"),
        };
        var logger = new FakeLogger<WeatherReportJob>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateJob(dbContext, new FakePdfGenerator(), emailSender, logger)
                .RunAsync(CancellationToken.None)
        );

        var log = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogEventIds.WeatherReportJob.ReportJobStarted, log.Id.Id);
    }
}
