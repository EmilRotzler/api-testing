using ApiTesting.Interfaces;
using ApiTesting.Managers;
using ApiTesting.Middleware;
using ApiTesting.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddScoped<IWeatherForecastManager, WeatherForecastManager>();

builder.Services.AddTransient<TokenAuthMiddleware>();

var app = builder.Build();

var authToken = app.Configuration["Auth:Token"];
if (string.IsNullOrEmpty(authToken))
{
    throw new InvalidOperationException(
        "Auth:Token is not configured. Set it in appsettings.json before starting the app.");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseMiddleware<TokenAuthMiddleware>();
app.MapControllers();

app.Run();
