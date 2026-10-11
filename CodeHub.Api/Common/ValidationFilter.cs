namespace CodeHub.Api.Common;

using CodeHub.Application.Common;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

/// <summary>
/// Endpoint filter that runs the registered <see cref="IValidator{T}"/> for a request DTO
/// and short-circuits with a 422 ApiResponse envelope when validation fails.
/// </summary>
public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();

        if (validator is not null)
        {
            var request = context.Arguments.OfType<T>().FirstOrDefault();

            if (request is not null)
            {
                var errors = validator.Validate(request);

                if (errors.Count > 0)
                {
                    var jsonOptions = context.HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value;

                    return Results.Json(
                        ApiResponse<object>.Fail(errors),
                        jsonOptions.SerializerOptions,
                        statusCode: 422);
                }
            }
        }

        return await next(context);
    }
}
