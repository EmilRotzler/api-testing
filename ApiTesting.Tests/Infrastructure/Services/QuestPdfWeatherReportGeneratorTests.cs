using System.Text;
using ApiTesting.Core.Models;
using ApiTesting.Infrastructure.Services;
using QuestPDF.Infrastructure;
using Xunit;

namespace ApiTesting.Tests.Infrastructure.Services;

public class QuestPdfWeatherReportGeneratorTests
{
    private static readonly DateTime GeneratedAt = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    public QuestPdfWeatherReportGeneratorTests()
    {
        // Program.cs sets this at startup; QuestPDF refuses to render without it.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static void AssertIsPdf(byte[] bytes)
    {
        Assert.NotEmpty(bytes);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Generate_ReturnsPdfForForecasts()
    {
        var forecasts = new[]
        {
            new WeatherForecast(new DateOnly(2026, 10, 2), 5, "Chilly")
            {
                Location = "Oslo",
                WindSpeedKmh = 20,
                WindDirectionDegrees = 270,
                HumidityPercent = 80,
                PrecipitationChancePercent = 40,
            },
            new WeatherForecast(new DateOnly(2026, 10, 3), 18, "Mild") { Location = "Bergen" },
        };

        var bytes = new QuestPdfWeatherReportGenerator().Generate(forecasts, GeneratedAt);

        AssertIsPdf(bytes);
    }

    [Fact]
    public void Generate_ReturnsPdfForEmptyList()
    {
        var bytes = new QuestPdfWeatherReportGenerator().Generate([], GeneratedAt);

        AssertIsPdf(bytes);
    }
}
