# ApiTesting

A minimal ASP.NET Core Web API built on .NET 10, used for exploring and testing REST API patterns. Exposes a single weather forecast endpoint backed by a manager/interface pattern.

## Project Structure

```
ApiTesting/
├── Controllers/
│   └── WeatherForecastController.cs   # GET /weatherforecast
├── Interfaces/
│   └── IWeatherForecastManager.cs     # Manager abstraction
├── Managers/
│   └── WeatherForecastManager.cs      # Business logic
├── Models/
│   └── WeatherForecast.cs             # Response model
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
| GET    | /weatherforecast   | Returns 5-day forecast with random temperatures and summaries |

### Example response

```json
[
  {
    "date": "2026-04-11",
    "temperatureC": 23,
    "temperatureF": 73,
    "summary": "Warm"
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
