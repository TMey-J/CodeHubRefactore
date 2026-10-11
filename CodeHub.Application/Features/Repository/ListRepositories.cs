namespace CodeHub.Application.Features.Repository;

using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;
using CodeHub.Domain.Entities;

public sealed record ListRepositoriesQuery(
    int? OwnerId,
    int Page,
    int Take,
    string? Search)
    : IQuery<PagedResult<Repository>>;

public sealed class ListRepositoriesHandler(IRepositoryRepository repositories)
    : IQueryHandler<ListRepositoriesQuery, PagedResult<Repository>>
{
    public Task<PagedResult<Repository>> HandleAsync(ListRepositoriesQuery query, CancellationToken cancellationToken = default) =>
        repositories.GetPagedAsync(query.OwnerId, query.Page, query.Take, query.Search, cancellationToken);
}
