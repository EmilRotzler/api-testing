using ApiTesting.Core.Models;

namespace ApiTesting.Core.Interfaces;

public interface IWeatherReportPdfGenerator
{
    byte[] Generate(IReadOnlyList<WeatherForecast> forecasts, DateTime generatedAtUtc);
}
