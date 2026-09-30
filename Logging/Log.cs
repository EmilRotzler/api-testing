namespace ApiTesting.Logging;

// Source-generated log messages. EventIds are allocated in LogEventIds.
// Never log passwords, password hashes or raw auth tokens.
internal static partial class Log
{
    // AuthController
    [LoggerMessage(EventId = LogEventIds.AuthController.LoginSucceeded, Level = LogLevel.Information,
        Message = "Login succeeded for {Username} from {IpAddress}")]
    public static partial void LoginRequestSucceeded(this ILogger logger, string username, string ipAddress);

    [LoggerMessage(EventId = LogEventIds.AuthController.LoginRejected, Level = LogLevel.Warning,
        Message = "Login rejected for {Username} from {IpAddress}")]
    public static partial void LoginRequestRejected(this ILogger logger, string username, string ipAddress);

    // AuthManager - login
    [LoggerMessage(EventId = LogEventIds.AuthManager.LoginSucceeded, Level = LogLevel.Information,
        Message = "Issued auth token for user {UserId}, expires at {ExpiresAt:O}")]
    public static partial void LoginSucceeded(this ILogger logger, int userId, DateTime expiresAt);

    [LoggerMessage(EventId = LogEventIds.AuthManager.LoginFailedUnknownUser, Level = LogLevel.Warning,
        Message = "Login failed for {Username}: unknown username")]
    public static partial void LoginFailedUnknownUser(this ILogger logger, string username);

    [LoggerMessage(EventId = LogEventIds.AuthManager.LoginFailedInactiveUser, Level = LogLevel.Warning,
        Message = "Login failed for {Username} (user {UserId}): account is inactive")]
    public static partial void LoginFailedInactiveUser(this ILogger logger, string username, int userId);

    [LoggerMessage(EventId = LogEventIds.AuthManager.LoginFailedInvalidPassword, Level = LogLevel.Warning,
        Message = "Login failed for {Username} (user {UserId}): invalid password")]
    public static partial void LoginFailedInvalidPassword(this ILogger logger, string username, int userId);

    // AuthManager - token validation
    [LoggerMessage(EventId = LogEventIds.AuthManager.TokenNotFound, Level = LogLevel.Warning,
        Message = "Token validation failed: token not found")]
    public static partial void TokenNotFound(this ILogger logger);

    [LoggerMessage(EventId = LogEventIds.AuthManager.TokenExpired, Level = LogLevel.Information,
        Message = "Token validation failed: token for user {UserId} expired at {ExpiresAt:O}")]
    public static partial void TokenExpired(this ILogger logger, int userId, DateTime expiresAt);

    // TokenAuthMiddleware
    [LoggerMessage(EventId = LogEventIds.TokenAuthMiddleware.RequestRejectedMissingToken, Level = LogLevel.Information,
        Message = "Rejected {Method} {Path} from {IpAddress}: no auth token")]
    public static partial void RequestRejectedMissingToken(this ILogger logger, string method, string path, string ipAddress);

    [LoggerMessage(EventId = LogEventIds.TokenAuthMiddleware.RequestRejectedInvalidToken, Level = LogLevel.Warning,
        Message = "Rejected {Method} {Path} from {IpAddress}: invalid or expired auth token")]
    public static partial void RequestRejectedInvalidToken(this ILogger logger, string method, string path, string ipAddress);

    // ExceptionHandlingMiddleware
    [LoggerMessage(EventId = LogEventIds.ExceptionHandlingMiddleware.UnhandledException, Level = LogLevel.Error,
        Message = "Unhandled exception processing {Method} {Path}")]
    public static partial void UnhandledException(this ILogger logger, Exception exception, string method, string path);

    [LoggerMessage(EventId = LogEventIds.ExceptionHandlingMiddleware.RequestAborted, Level = LogLevel.Information,
        Message = "Request {Method} {Path} was aborted by the client")]
    public static partial void RequestAborted(this ILogger logger, string method, string path);

    [LoggerMessage(EventId = LogEventIds.ExceptionHandlingMiddleware.ResponseAlreadyStarted, Level = LogLevel.Error,
        Message = "Unhandled exception processing {Method} {Path} after the response started; cannot send an error response")]
    public static partial void ResponseAlreadyStarted(this ILogger logger, Exception exception, string method, string path);

    // WeatherForecastController
    [LoggerMessage(EventId = LogEventIds.WeatherForecastController.ForecastRequested, Level = LogLevel.Debug,
        Message = "Weather forecast requested")]
    public static partial void ForecastRequested(this ILogger logger);

    [LoggerMessage(EventId = LogEventIds.WeatherForecastController.ForecastCacheInvalidated, Level = LogLevel.Information,
        Message = "Weather forecast cache invalidated")]
    public static partial void ForecastCacheInvalidated(this ILogger logger);
}
