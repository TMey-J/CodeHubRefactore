namespace CodeHub.Application.Features.Repository;

using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;
using CodeHub.Domain.Entities;

public sealed record SearchRepositoriesQuery(string Query)
    : IQuery<IReadOnlyList<Repository>>;

public sealed class SearchRepositoriesHandler(
    IRepositoryRepository repositories,
    IAiClient aiClient,
    IAiRequestRepository aiRequests)
    : IQueryHandler<SearchRepositoriesQuery, IReadOnlyList<Repository>>
{
    public async Task<IReadOnlyList<Repository>> HandleAsync(SearchRepositoriesQuery query, CancellationToken cancellationToken = default)
    {
        var repositoriesData = await repositories.GetAllPublicAsync(cancellationToken);

        await aiRequests.AddAsync(null, AiServiceNames.RepositorySearch, cancellationToken);

        var items = repositoriesData
            .Select(r => new RepositorySearchItem(r.Id, r.Name, r.Description, r.Language, r.Owner.Username))
            .ToList();

        var matchingIds = await aiClient.SearchRepositoriesAsync(query.Query, items, cancellationToken);

        return repositoriesData
            .Where(r => matchingIds.Contains(r.Id))
            .ToList();
    }
}
