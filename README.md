# ApiTesting

A small ASP.NET Core Web API built on .NET 10 for exploring and testing REST API patterns. It serves weather forecast data and includes the building blocks most real APIs need:

- **Layered architecture**: controllers, managers and services are separated into Api, Core and Infrastructure layers and wired up through interfaces and dependency injection.
- **Persistence**: SQLite via EF Core, with migrations applied and seed data inserted automatically on startup.
- **Authentication**: username/password login with Argon2id password hashing, issuing a token in an `HttpOnly` cookie that middleware validates on every protected request.
- **Rate limiting**: login attempts are limited to 5 per minute per IP address.
- **Caching**: forecast results are cached in memory for 30 seconds, with an endpoint to invalidate the cache.
- **Error handling**: global exception middleware turns unhandled errors into consistent JSON responses.
- **Structured logging**: source-generated log messages with allocated event IDs, with readable console output in Development and JSON elsewhere.
- **Tests**: an xUnit test project covering every layer, including assertions on log output.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Getting Started

### Configuration

`appsettings.json` is gitignored. Copy the example file to get started:

```bash
cp appsettings.example.json appsettings.json
```

If you already have an `appsettings.json`, copy the `ConnectionStrings:Hangfire` and `Email` entries from the example into it.

### Running the API

```bash
dotnet run
```

The API starts on:
- HTTP: `http://localhost:5095`
- HTTPS: `https://localhost:7254`

## Authentication

Endpoints require an authenticated user by default. Log in with `POST /auth/login`; on success the API sets an `HttpOnly` `AuthToken` cookie that must be sent on subsequent requests. Tokens expire after `Auth:TokenLifetimeHours` (default 24).

```
POST /auth/login
Content-Type: application/json

{ "username": "admin", "password": "admin123" }
```

Requests without a valid token receive `401 Unauthorized`. `GET /weatherforecast` is marked `[AllowAnonymous]` and does not require a token; `POST /weatherforecast/invalidate-cache` does. Mark other endpoints with `[AllowAnonymous]` (`Microsoft.AspNetCore.Authorization`) to exclude them the same way.

### Default user

To make basic testing possible out of the box, the database is created with one admin user:

| Username | Password   |
|----------|------------|
| `admin`  | `admin123` |

> **Note:** These credentials are public. If you use this project for anything beyond local testing, change the admin password (or replace the user) in the database. Changing it won't recreate the default admin; the seed only runs again if `app.db` is deleted.

## Endpoints

| Method | Route                              | Auth          | Description |
|--------|------------------------------------|---------------|-------------|
| GET    | /weatherforecast                   | Anonymous     | Returns the 5 persisted forecast rows from the database |
| POST   | /weatherforecast/invalidate-cache  | Authenticated | Clears the 30s forecast cache |
| POST   | /weatherforecast/email-report      | Authenticated | Queues a background job that emails a PDF of the forecasts; returns `202` with `{ "jobId": "..." }` |
| GET    | /hangfire                          | Admin role    | Hangfire background-job dashboard |

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

## Email reports

`POST /weatherforecast/email-report` enqueues a [Hangfire](https://www.hangfire.io/) job and returns immediately. The job reads the current forecasts from the database, renders a one-page PDF with [QuestPDF](https://www.questpdf.com/) (Community license) and emails it via SMTP ([MailKit](https://github.com/jstedfast/MailKit)) to the address in `Email:ReportRecipient`.

Jobs are stored in a separate SQLite file (`ConnectionStrings:Hangfire`, default `hangfire.db`), so queued jobs survive restarts. Hangfire polls for new jobs, so expect a delay of up to ~15 seconds before a job runs. Failed jobs are retried up to 3 times and then appear under *Failed* in the dashboard, where they can be requeued.

### Configuration

| Key | Description |
|-----|-------------|
| `Email:From` | Sender address |
| `Email:ReportRecipient` | Where reports are sent |
| `Email:Smtp:Host` / `Port` | SMTP server (default `localhost:1025`) |
| `Email:Smtp:UseSsl` | `true` to negotiate SSL/STARTTLS; `false` for local catchers |
| `Email:Smtp:Username` / `Password` | Optional; authentication is skipped when `Username` is empty |

Invalid email settings stop the app at startup.

### Testing email locally

No Docker needed. [smtp4dev](https://github.com/rnwood/smtp4dev) is installed as a local tool by `dotnet tool restore`:

```bash
dotnet smtp4dev --smtpport=1025 --urls=http://localhost:5000
```

Trigger a report, then view the received email and PDF at `http://localhost:5000`.

Prefer Docker? [Mailpit](https://mailpit.axllent.org/) works the same way: `docker run -p 8025:8025 -p 1025:1025 axllent/mailpit` (UI at `http://localhost:8025`).

### Dashboard

`/hangfire` requires a logged-in user with the `Admin` role (the seeded `admin` user qualifies). Open `http://localhost:5095/hangfire`: if you aren't logged in as an admin you get a minimal login page (`wwwroot/hangfire/login.html`) at the same URL, and after logging in it shows the dashboard. Other dashboard URLs return `401` until you're logged in.

## OpenAPI / Swagger

OpenAPI metadata is available in development at:

```
http://localhost:5095/openapi/v1.json
```

## Testing

The project is built with testing in mind. Business logic sits behind interfaces, so every layer can be tested in isolation.

### Automated tests

The `ApiTesting.Tests` project uses xUnit and mirrors the app's folder layout. Run it with:

```bash
dotnet test ApiTesting.Tests
```

- **Unit tests span every layer**: controllers, managers, middleware and services.
- **Database access** runs against the EF Core in-memory provider, so no SQLite file is needed.
- **Logging** is verified in the same tests using `FakeLogger`, including checks that passwords and tokens are never logged.

### Manual testing

[ApiTesting.http](ApiTesting.http) contains ready-made requests that can be run against a local instance directly from VS Code (REST Client extension) or Visual Studio.

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

## Project Structure

```
ApiTesting/
├── Api/                   # HTTP layer
│   ├── Controllers/       # API endpoints (auth, weather forecast)
│   ├── Dtos/              # Request/response types
│   ├── Filters/           # Hangfire dashboard authorization
│   └── Middleware/        # Exception handling, token auth
├── Core/                  # Business logic & domain
│   ├── Interfaces/        # Manager and service abstractions
│   ├── Jobs/              # Hangfire background jobs
│   ├── Managers/          # Business logic
│   └── Models/            # Domain / EF entities
├── Infrastructure/        # External concerns
│   ├── Data/              # EF Core DbContext
│   ├── Migrations/        # EF Core migrations
│   └── Services/          # Password hashing, caching, PDF, SMTP
├── Common/                # Cross-cutting
│   ├── Configuration/     # Strongly typed options
│   ├── Constants/         # Shared constants
│   └── Logging/           # Log messages and event IDs
├── ApiTesting.Tests/      # Test project (mirrors the layout above)
├── Program.cs             # App bootstrap & DI registration
└── ApiTesting.http        # HTTP test file
```
