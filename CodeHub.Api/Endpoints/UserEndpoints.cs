namespace CodeHub.Api.Endpoints;

using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;
using CodeHub.Application.Contracts;
using CodeHub.Application.Features.User;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/user").WithTags("User");

        group.MapGet("/profile/{username}",
            async (string username, int page, int take, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            {
                var profile = await dispatcher.QueryAsync<GetUserProfileQuery, UserProfileResponse>(
                    new GetUserProfileQuery(username, page, take),
                    cancellationToken);

                return TypedResults.Ok(ApiResponse<UserProfileResponse>.Ok(profile));
            });

        return app;
    }
}
