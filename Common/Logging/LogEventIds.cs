namespace ApiTesting.Common.Logging;

// EventId allocation: each area owns a block of 1000, each component a block of 100 within it.
//   1000-1999  Authentication
//     1100-1199  AuthController
//     1200-1299  AuthManager
//     1300-1399  TokenAuthMiddleware
//   2000-2999  Weather forecasts
//     2100-2199  WeatherForecastController
//   9000-9999  Infrastructure
//     9100-9199  ExceptionHandlingMiddleware
public static class LogEventIds
{
    public static class ExceptionHandlingMiddleware
    {
        public const int UnhandledException = 9100;
        public const int RequestAborted = 9101;
        public const int ResponseAlreadyStarted = 9102;
    }

    public static class AuthController
    {
        public const int LoginSucceeded = 1100;
        public const int LoginRejected = 1101;
    }

    public static class AuthManager
    {
        public const int LoginSucceeded = 1200;
        public const int LoginFailedUnknownUser = 1201;
        public const int LoginFailedInactiveUser = 1202;
        public const int LoginFailedInvalidPassword = 1203;
        public const int TokenNotFound = 1210;
        public const int TokenExpired = 1211;
    }

    public static class TokenAuthMiddleware
    {
        public const int RequestRejectedMissingToken = 1300;
        public const int RequestRejectedInvalidToken = 1301;
    }

    public static class WeatherForecastController
    {
        public const int ForecastRequested = 2100;
        public const int ForecastCacheInvalidated = 2101;
    }
}
