namespace ApiTesting.Models;

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int Id { get; init; }

    public string Location { get; init; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public int WindSpeedKmh { get; init; }

    public int WindDirectionDegrees { get; init; }

    public int HumidityPercent { get; init; }

    public int PrecipitationChancePercent { get; init; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    /// Wind chill (Environment Canada / NWS metric formula); falls back to the air
    /// temperature when the formula's validity range (&lt;=10C, wind &gt;4.8km/h) isn't met.
    public double FeelsLikeC =>
        TemperatureC <= 10 && WindSpeedKmh > 4.8
            ? 13.12 + (0.6215 * TemperatureC) - (11.37 * Math.Pow(WindSpeedKmh, 0.16))
                + (0.3965 * TemperatureC * Math.Pow(WindSpeedKmh, 0.16))
            : TemperatureC;
}
