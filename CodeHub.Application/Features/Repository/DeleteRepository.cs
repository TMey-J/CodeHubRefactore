namespace CodeHub.Application.Features.Repository;

using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;

public sealed record DeleteRepositoryCommand(int OwnerId, int Id)
    : ICommand;

public sealed class DeleteRepositoryHandler(IRepositoryRepository repositories, IFileStorage storage)
    : ICommandHandler<DeleteRepositoryCommand>
{
    public async Task HandleAsync(DeleteRepositoryCommand command, CancellationToken cancellationToken = default)
    {
        var repository = await repositories.GetByIdAsync(command.Id, command.OwnerId, cancellationToken)
            ?? throw AppException.BadRequest("repository not found exists");

        await storage.DeleteRepositoryDirectoryAsync(repository.Id);

        await repositories.RemoveAsync(repository, cancellationToken);
    }
}
