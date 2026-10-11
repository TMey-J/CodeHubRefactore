namespace CodeHub.Application.Common;

/// <summary>
/// Application level failure carrying the HTTP status the legacy API contract expects.
/// </summary>
public class AppException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public static AppException BadRequest(string message) => new(message, 400);

    public static AppException Unauthorized(string message) => new(message, 401);

    public static AppException NotFound(string message) => new(message, 404);

    public static AppException TooManyRequests(string message) => new(message, 429);
}

/// <summary>
/// Thrown when every SambaNova API key has exhausted its daily quota.
/// </summary>
public sealed class SambaNovaDailyLimitException()
    : AppException("All SambaNova API Keys have reached their daily limit.", 429);
