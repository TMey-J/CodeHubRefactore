namespace CodeHub.Api.Common;

using CodeHub.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

/// <summary>
/// Maps unhandled exceptions onto the legacy ApiResponse envelope:
/// AppException keeps its status/message, everything else is a 500 ("Somthing Wrong"
/// is the exact legacy message and is kept for client compatibility).
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, errors) = exception switch
        {
            AppException appException => (appException.StatusCode, new List<string> { appException.Message }),
            _ => (StatusCodes.Status500InternalServerError, new List<string> { "Somthing Wrong" })
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);
        }

        var jsonOptions = httpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value;

        var result = Results.Json(
            ApiResponse<object>.Fail(errors),
            jsonOptions.SerializerOptions,
            statusCode: statusCode);

        await result.ExecuteAsync(httpContext);

        return true;
    }
}
