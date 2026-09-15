using ApiTesting.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiTesting.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();

    public DbSet<User> Users => Set<User>();

    public DbSet<AuthToken> AuthTokens => Set<AuthToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WeatherForecast>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Ignore(f => f.TemperatureF);
            entity.Ignore(f => f.FeelsLikeC);

            entity.HasData(
                new WeatherForecast(new DateOnly(2026, 9, 16), 20, "Mild")
                {
                    Id = 1,
                    Location = "Oslo",
                    Latitude = 59.9139,
                    Longitude = 10.7522,
                    WindSpeedKmh = 12,
                    WindDirectionDegrees = 270,
                    HumidityPercent = 60,
                    PrecipitationChancePercent = 20,
                },
                new WeatherForecast(new DateOnly(2026, 9, 17), 23, "Warm")
                {
                    Id = 2,
                    Location = "Paris",
                    Latitude = 48.8566,
                    Longitude = 2.3522,
                    WindSpeedKmh = 8,
                    WindDirectionDegrees = 180,
                    HumidityPercent = 55,
                    PrecipitationChancePercent = 10,
                },
                new WeatherForecast(new DateOnly(2026, 9, 18), 15, "Cool")
                {
                    Id = 3,
                    Location = "Berlin",
                    Latitude = 52.5200,
                    Longitude = 13.4050,
                    WindSpeedKmh = 18,
                    WindDirectionDegrees = 45,
                    HumidityPercent = 70,
                    PrecipitationChancePercent = 40,
                },
                new WeatherForecast(new DateOnly(2026, 9, 19), 5, "Chilly")
                {
                    Id = 4,
                    Location = "Madrid",
                    Latitude = 40.4168,
                    Longitude = -3.7038,
                    WindSpeedKmh = 25,
                    WindDirectionDegrees = 315,
                    HumidityPercent = 80,
                    PrecipitationChancePercent = 60,
                },
                new WeatherForecast(new DateOnly(2026, 9, 20), 30, "Hot")
                {
                    Id = 5,
                    Location = "Rome",
                    Latitude = 41.9028,
                    Longitude = 12.4964,
                    WindSpeedKmh = 5,
                    WindDirectionDegrees = 90,
                    HumidityPercent = 35,
                    PrecipitationChancePercent = 5,
                }
            );
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasData(new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@example.com",
                FirstName = "Admin",
                LastName = "User",
                PasswordHash = "zVX425OzpyPcasd3oSsCCA==:FjWNWFf7HhMaucTg5sK3rV56qw9udvVeqvKPM4tssyA=",
                Role = "Admin",
                IsActive = true,
                CreatedAt = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
                LastLoginAt = null,
            });
        });

        modelBuilder.Entity<AuthToken>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.Token).IsUnique();
            entity.HasOne<User>().WithMany().HasForeignKey(t => t.UserId);
        });
    }
}
