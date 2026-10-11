namespace CodeHub.Api.Endpoints;

using CodeHub.Api.Common;

using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;
using CodeHub.Application.Common.Mapping;
using CodeHub.Application.Contracts;
using CodeHub.Application.Features.Repository;
using CodeHub.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

public static class RepositoryEndpoints
{
    public static IEndpointRouteBuilder MapRepositoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/repo").WithTags("Repository");

        group.MapPost("",
            async (CreateRepositoryRequest request, IDispatcher dispatcher, ICurrentUser currentUser,
                CancellationToken cancellationToken) =>
            {
                var user = await currentUser.GetUserAsync(cancellationToken);

                var repository = await dispatcher.SendAsync<CreateRepositoryCommand, Repository>(
                    new CreateRepositoryCommand(user.Id, request.Name, request.Description, request.Visibility, request.Language),
                    cancellationToken);

                return TypedResults.Ok(ApiResponse<RepositoryResponse>.Ok(repository.ToResponse()));
            })
            .AddEndpointFilter<ValidationFilter<CreateRepositoryRequest>>()
            .RequireAuthorization();

        group.MapPut("",
            async (UpdateRepositoryRequest request, IDispatcher dispatcher, ICurrentUser currentUser,
                CancellationToken cancellationToken) =>
            {
                var user = await currentUser.GetUserAsync(cancellationToken);

                var repository = await dispatcher.SendAsync<UpdateRepositoryCommand, Repository>(
                    new UpdateRepositoryCommand(user.Id, request.Id, request.Name, request.Description, request.Visibility, request.Language),
                    cancellationToken);

                return TypedResults.Ok(ApiResponse<RepositoryResponse>.Ok(repository.ToResponse()));
            })
            .AddEndpointFilter<ValidationFilter<UpdateRepositoryRequest>>()
            .RequireAuthorization();

        group.MapDelete("",
            async ([FromBody] DeleteRepositoryRequest request, IDispatcher dispatcher, ICurrentUser currentUser,
                CancellationToken cancellationToken) =>
            {
                var user = await currentUser.GetUserAsync(cancellationToken);

                await dispatcher.SendAsync(new DeleteRepositoryCommand(user.Id, request.Id), cancellationToken);

                return TypedResults.Ok(ApiResponse<object>.Ok());
            })
            .RequireAuthorization();

        group.MapGet("",
            async (int page, int take, string? search, IDispatcher dispatcher, ICurrentUser currentUser,
                CancellationToken cancellationToken) =>
            {
                var user = await currentUser.GetUserAsync(cancellationToken);
                return TypedResults.Ok(await ListRepositoriesAsync(dispatcher, user.Id, page, take, search, cancellationToken));
            })
            .RequireAuthorization();

        group.MapGet("/getAll",
            async (int page, int take, string? search, IDispatcher dispatcher, CancellationToken cancellationToken) =>
                TypedResults.Ok(await ListRepositoriesAsync(dispatcher, null, page, take, search, cancellationToken)));

        group.MapPost("/search",
            async (SearchRepositoriesRequest request, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            {
                var repositories = await dispatcher.QueryAsync<SearchRepositoriesQuery, IReadOnlyList<Repository>>(
                    new SearchRepositoriesQuery(request.Query),
                    cancellationToken);

                return TypedResults.Ok(ApiResponse<IReadOnlyList<RepositoryResponse>>.Ok(
                    repositories.Select(r => r.ToResponse()).ToList()));
            })
            .AddEndpointFilter<ValidationFilter<SearchRepositoriesRequest>>();

        group.MapPost("/readme",
            async (GenerateReadmeRequest request, IDispatcher dispatcher, ICurrentUser currentUser,
                CancellationToken cancellationToken) =>
            {
                var user = await currentUser.GetUserAsync(cancellationToken);

                var result = await dispatcher.SendAsync<GenerateReadmeCommand, GenerateReadmeResponse>(
                    new GenerateReadmeCommand(user.Id, request.RepositoryId, request.EnResponse),
                    cancellationToken);

                return TypedResults.Ok(ApiResponse<GenerateReadmeResponse>.Ok(result));
            })
            .RequireAuthorization();

        group.MapPost("/{repositoryId:int}/star",
            async (int repositoryId, IDispatcher dispatcher, ICurrentUser currentUser,
                CancellationToken cancellationToken) =>
            {
                var user = await currentUser.GetUserAsync(cancellationToken);

                var result = await dispatcher.SendAsync<ToggleStarCommand, ToggleStarResponse>(
                    new ToggleStarCommand(user.Id, repositoryId),
                    cancellationToken);

                return TypedResults.Ok(ApiResponse<ToggleStarResponse>.Ok(result));
            })
            .RequireAuthorization();

        group.MapGet("/{repositoryId:int}/isStarred",
            async (int repositoryId, IDispatcher dispatcher, ICurrentUser currentUser,
                CancellationToken cancellationToken) =>
            {
                var user = await currentUser.GetUserAsync(cancellationToken);

                var isStarred = await dispatcher.QueryAsync<CheckStarQuery, bool>(
                    new CheckStarQuery(user.Id, repositoryId),
                    cancellationToken);

                return TypedResults.Ok(ApiResponse<bool>.Ok(isStarred));
            })
            .RequireAuthorization();

        group.MapGet("/{ownerName}/{repositoryName}",
            async (string ownerName, string repositoryName, IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var repository = await dispatcher.QueryAsync<GetRepositoryByNameQuery, Repository>(
                    new GetRepositoryByNameQuery(ownerName, repositoryName),
                    cancellationToken);

                return TypedResults.Ok(ApiResponse<RepositoryResponse>.Ok(repository.ToResponse()));
            });

        return app;
    }

    private static async Task<ApiResponse<PagedResponse<RepositoryResponse>>> ListRepositoriesAsync(
        IDispatcher dispatcher,
        int? ownerId,
        int page,
        int take,
        string? search,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.QueryAsync<ListRepositoriesQuery, PagedResult<Repository>>(
            new ListRepositoriesQuery(ownerId, page, take, search),
            cancellationToken);

        var items = result.Items.Select(r => r.ToResponse()).ToList();

        return ApiResponse<PagedResponse<RepositoryResponse>>.Ok(
            PagedResponse<RepositoryResponse>.Create(items, result.TotalCount, page, take));
    }
}
