namespace CodeHub.Application.Features.User;

using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;
using CodeHub.Application.Common.Mapping;
using CodeHub.Application.Contracts;

public sealed record GetUserProfileQuery(string Username, int Page, int Take)
    : IQuery<UserProfileResponse>;

public sealed class GetUserProfileHandler(IUserRepository users)
    : IQueryHandler<GetUserProfileQuery, UserProfileResponse>
{
    public async Task<UserProfileResponse> HandleAsync(GetUserProfileQuery query, CancellationToken cancellationToken = default)
    {
        var profile = await users.GetProfileAsync(query.Username, query.Page, query.Take, cancellationToken)
            ?? throw AppException.NotFound("User not found");

        var items = profile.PublicRepositories
            .Select(r => r.ToResponse())
            .ToList();

        return new UserProfileResponse(
            profile.User.Username,
            profile.User.CreatedAt.ToDisplayString(),
            profile.PublicRepositoriesCount,
            profile.PublicFilesCount,
            profile.ReceivedStars,
            PagedResponse<RepositoryResponse>.Create(items, profile.PublicRepositoriesCount, query.Page, query.Take));
    }
}
