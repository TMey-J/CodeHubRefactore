namespace CodeHub.Application.Features.Repository;

using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;
using CodeHub.Domain.Entities;

public sealed record GetRepositoryByNameQuery(string OwnerName, string RepositoryName)
    : IQuery<Repository>;

public sealed class GetRepositoryByNameHandler(IRepositoryRepository repositories)
    : IQueryHandler<GetRepositoryByNameQuery, Repository>
{
    public async Task<Repository> HandleAsync(GetRepositoryByNameQuery query, CancellationToken cancellationToken = default) =>
        await repositories.GetByNameAsync(query.RepositoryName, ownerName: query.OwnerName, cancellationToken: cancellationToken)
            ?? throw AppException.NotFound("repository not found");
}
