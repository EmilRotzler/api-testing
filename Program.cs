using System.ComponentModel.DataAnnotations;
using ApiTesting.Api.Filters;
using ApiTesting.Api.Middleware;
using ApiTesting.Common.Configuration;
using ApiTesting.Core.Jobs;
using ApiTesting.Core.Interfaces;
using ApiTesting.Core.Managers;
using ApiTesting.Infrastructure.Data;
using ApiTesting.Infrastructure.Services;
using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Console only, so the app runs locally with no external log service. Format and levels come from
// the "Logging" section of appsettings: readable single-line output in Development, JSON elsewhere.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddScoped<IWeatherForecastManager, WeatherForecastManager>();
builder.Services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
builder.Services.AddScoped<IAuthManager, AuthManager>();

// QuestPDF Community license: free for individuals and organisations under USD 1M annual revenue.
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection(EmailOptions.SectionName))
    .ValidateDataAnnotations()
    // DataAnnotations validation doesn't recurse into nested objects, so validate Smtp explicitly.
    .Validate(
        options => Validator.TryValidateObject(
            options.Smtp, new ValidationContext(options.Smtp), null, validateAllProperties: true),
        "Email:Smtp configuration is invalid")
    .ValidateOnStart();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<IWeatherReportPdfGenerator, QuestPdfWeatherReportGenerator>();
builder.Services.AddScoped<WeatherReportJob>();

// Hangfire keeps its own tables in a separate SQLite file so they stay out of the EF migrations.
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSQLiteStorage(builder.Configuration.GetConnectionString("Hangfire") ?? "hangfire.db"));
builder.Services.AddHangfireServer();

builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            """{"error": "Too many requests, please try again later"}""",
            cancellationToken);
    };

    options.AddPolicy("login", httpContext =>
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });

    options.AddPolicy("standard", httpContext =>
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddTransient<ExceptionHandlingMiddleware>();
builder.Services.AddTransient<TokenAuthMiddleware>();
builder.Services.AddTransient<DashboardLoginMiddleware>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

// First in the pipeline so it catches exceptions from everything registered after it.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
// Before routing/auth so wwwroot (the Hangfire login page) is served without a token.
app.UseMiddleware<DashboardLoginMiddleware>();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseMiddleware<TokenAuthMiddleware>();
app.MapControllers();

// TokenAuthMiddleware rejects requests without a valid cookie first; the filter then requires the Admin role.
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [],
    AsyncAuthorization = [new AdminDashboardAuthorizationFilter()],
});

app.Run();
