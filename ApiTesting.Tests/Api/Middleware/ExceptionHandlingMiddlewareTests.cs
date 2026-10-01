using System.Text.Json;
using ApiTesting.Api.Middleware;
using ApiTesting.Common.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace ApiTesting.Tests.Api.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/weatherforecast";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonElement> ReadJsonBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
    }

    [Fact]
    public async Task InvokeAsync_PassesThroughWhenNoException()
    {
        var logger = new FakeLogger<ExceptionHandlingMiddleware>();
        var middleware = new ExceptionHandlingMiddleware(logger);
        var context = CreateContext();

        await middleware.InvokeAsync(context, ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        Assert.Empty(logger.Collector.GetSnapshot());
    }

    [Fact]
    public async Task InvokeAsync_ReturnsInternalServerErrorWithoutExceptionDetails()
    {
        var logger = new FakeLogger<ExceptionHandlingMiddleware>();
        var middleware = new ExceptionHandlingMiddleware(logger);
        var context = CreateContext();
        var exception = new InvalidOperationException("database password is hunter2");

        await middleware.InvokeAsync(context, _ => throw exception);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        var body = await ReadJsonBody(context);
        Assert.Equal("An unexpected error occurred", body.GetProperty("error").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("traceId").GetString()));
        Assert.DoesNotContain("hunter2", body.GetRawText());

        var log = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Error, log.Level);
        Assert.Equal(LogEventIds.ExceptionHandlingMiddleware.UnhandledException, log.Id.Id);
        Assert.Same(exception, log.Exception);
        Assert.Contains("GET /weatherforecast", log.Message);
    }

    [Fact]
    public async Task InvokeAsync_DoesNotWriteErrorWhenClientAborted()
    {
        var logger = new FakeLogger<ExceptionHandlingMiddleware>();
        var middleware = new ExceptionHandlingMiddleware(logger);
        var context = CreateContext();
        using var abort = new CancellationTokenSource();
        context.RequestAborted = abort.Token;
        abort.Cancel();

        await middleware.InvokeAsync(context, ctx => throw new OperationCanceledException(ctx.RequestAborted));

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);

        var log = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, log.Level);
        Assert.Equal(LogEventIds.ExceptionHandlingMiddleware.RequestAborted, log.Id.Id);
    }
}
