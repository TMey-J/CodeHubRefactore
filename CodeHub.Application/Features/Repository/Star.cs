namespace CodeHub.Application.Features.Repository;

using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;
using CodeHub.Application.Contracts;

public sealed record ToggleStarCommand(int UserId, int RepositoryId)
    : ICommand<ToggleStarResponse>;

public sealed class ToggleStarHandler(IRepositoryRepository repositories, IStarRepository stars)
    : ICommandHandler<ToggleStarCommand, ToggleStarResponse>
{
    public async Task<ToggleStarResponse> HandleAsync(ToggleStarCommand command, CancellationToken cancellationToken = default)
    {
        var repository = await repositories.GetByIdAsync(command.RepositoryId, cancellationToken: cancellationToken)
            ?? throw AppException.BadRequest("Repository not found");

        if (await stars.ExistsAsync(repository.Id, command.UserId, cancellationToken))
        {
            await stars.RemoveAsync(repository.Id, command.UserId, cancellationToken);
            return new ToggleStarResponse(false, repository.StarsCount - 1);
        }

        if (repository.OwnerId == command.UserId)
        {
            throw AppException.BadRequest("Cannot add star to your repository");
        }

        await stars.AddAsync(repository.Id, command.UserId, cancellationToken);
        return new ToggleStarResponse(true, repository.StarsCount + 1);
    }
}

public sealed record CheckStarQuery(int UserId, int RepositoryId)
    : IQuery<bool>;

public sealed class CheckStarHandler(IStarRepository stars)
    : IQueryHandler<CheckStarQuery, bool>
{
    public Task<bool> HandleAsync(CheckStarQuery query, CancellationToken cancellationToken = default) =>
        stars.ExistsAsync(query.RepositoryId, query.UserId, cancellationToken);
}
