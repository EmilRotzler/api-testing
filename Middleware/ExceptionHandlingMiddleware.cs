using System.Diagnostics;
using ApiTesting.Logging;

namespace ApiTesting.Middleware;

public class ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client went away; there is nobody to send an error response to.
            logger.RequestAborted(context.Request.Method, context.Request.Path);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                logger.ResponseAlreadyStarted(exception, context.Request.Method, context.Request.Path);
                throw;
            }

            logger.UnhandledException(exception, context.Request.Method, context.Request.Path);
            await WriteInternalServerError(context);
        }
    }

    private static async Task WriteInternalServerError(HttpContext context)
    {
        // Never expose exception details to the client; the traceId links the response to the log entry.
        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "An unexpected error occurred",
            traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier,
        });
    }
}
