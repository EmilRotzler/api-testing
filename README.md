# ApiTesting

A minimal ASP.NET Core Web API built on .NET 10, used for exploring and testing REST API patterns. Exposes a single weather forecast endpoint backed by a manager/interface pattern.

## Project Structure

```
ApiTesting/
├── Controllers/
│   └── WeatherForecastController.cs   # GET /weatherforecast
├── Data/
│   └── AppDbContext.cs                # EF Core DbContext
├── Interfaces/
│   └── IWeatherForecastManager.cs     # Manager abstraction
├── Managers/
│   └── WeatherForecastManager.cs      # Business logic
├── Migrations/                        # EF Core migrations
├── Models/
│   └── WeatherForecast.cs             # Response model / EF entity
├── Program.cs                         # App bootstrap & DI registration
└── ApiTesting.http                    # HTTP test file
```

## Configuration

`appsettings.json` is gitignored. Copy the example file to get started:

```bash
cp appsettings.example.json appsettings.json
```

## Authentication

Endpoints require a bearer token by default. Set `Auth:Token` in `appsettings.json` (the app fails to start if it's missing), then send it on protected requests:

```
Authorization: Bearer <your-token>
```

Requests without a valid token receive `401 Unauthorized`. `GET /weatherforecast` is marked `[AllowAnonymous]` and does not require a token; `POST /weatherforecast/invalidate-cache` does. Mark other endpoints with `[AllowAnonymous]` (`Microsoft.AspNetCore.Authorization`) to exclude them the same way.

## Database

Forecast data is persisted in a local SQLite database (`app.db`, gitignored). The
connection string lives under `ConnectionStrings:Default` in `appsettings.json`.

On startup, the app automatically creates `app.db` and applies any pending EF Core
migrations (including seeding the initial forecast rows) via `Database.Migrate()` — no
manual setup is required to run the app.

To add a new migration after changing an entity, use the `dotnet-ef` local tool
(already restored via `dotnet tool restore`):

```bash
dotnet ef migrations add <MigrationName>
```

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Running the API

```bash
dotnet run
```

The API starts on:
- HTTP: `http://localhost:5095`
- HTTPS: `https://localhost:7254`

## Endpoints

| Method | Route              | Description                          |
|--------|--------------------|--------------------------------------|
| GET    | /weatherforecast   | Returns the 5 persisted forecast rows from the database |

### Example response

```json
[
  {
    "date": "2026-09-16",
    "temperatureC": 20,
    "summary": "Mild",
    "id": 1,
    "location": "Oslo",
    "latitude": 59.9139,
    "longitude": 10.7522,
    "windSpeedKmh": 12,
    "windDirectionDegrees": 270,
    "humidityPercent": 60,
    "precipitationChancePercent": 20,
    "temperatureF": 67,
    "feelsLikeC": 20
  }
]
```

## OpenAPI / Swagger

OpenAPI metadata is available in development at:

```
http://localhost:5095/openapi/v1.json
```

## Testing with the .http file

The included [ApiTesting.http](ApiTesting.http) file can be run directly in VS Code (with the REST Client extension) or Visual Studio:

```
GET http://localhost:5095/weatherforecast/
Accept: application/json
```
