using ApiTesting.Interfaces;
using ApiTesting.Models;

namespace ApiTesting.Managers;

public class WeatherForecastManager : IWeatherForecastManager
{
    private static readonly string[] Summaries =
    [
        "Freezing",
        "Bracing",
        "Chilly",
        "Cool",
        "Mild",
        "Warm",
        "Balmy",
        "Hot",
        "Sweltering",
        "Scorching",
    ];

    public IEnumerable<WeatherForecast> GetForecast()
    {
        return Enumerable
            .Range(1, 5)
            .Select(index => new WeatherForecast(
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                Summaries[Random.Shared.Next(Summaries.Length)]
            ));
    }
}
