namespace CodeHub.Api.Endpoints;

using CodeHub.Api.Common;

using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;
using CodeHub.Application.Common.Mapping;
using CodeHub.Application.Contracts;
using CodeHub.Application.Features.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Authentication");

        group.MapPost("/register",
            async (RegisterRequest request, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            {
                var user = await dispatcher.SendAsync<RegisterUserCommand, UserResponse>(
                    new RegisterUserCommand(request.Username, request.Email, request.Password),
                    cancellationToken);

                return TypedResults.Ok(ApiResponse<UserResponse>.Ok(user));
            })
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>();

        group.MapPost("/login",
            async (LoginRequest request, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            {
                var tokens = await dispatcher.SendAsync<LoginUserCommand, TokenResponse>(
                    new LoginUserCommand(request.Username, request.Password),
                    cancellationToken);

                return TypedResults.Ok(ApiResponse<TokenResponse>.Ok(tokens));
            })
            .AddEndpointFilter<ValidationFilter<LoginRequest>>();

        group.MapGet("/user-info",
            async (ICurrentUser currentUser, CancellationToken cancellationToken) =>
            {
                var user = await currentUser.GetUserAsync(cancellationToken);
                return TypedResults.Ok(user.ToResponse());
            })
            .RequireAuthorization();

        return app;
    }
}
