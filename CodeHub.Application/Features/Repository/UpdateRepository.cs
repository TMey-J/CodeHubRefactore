namespace CodeHub.Application.Features.Repository;

using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;
using CodeHub.Domain.Entities;
using CodeHub.Domain.Enums;

public sealed record UpdateRepositoryCommand(
    int OwnerId,
    int Id,
    string Name,
    string? Description,
    RepositoryVisibility Visibility,
    string Language)
    : ICommand<Repository>;

public sealed class UpdateRepositoryHandler(IRepositoryRepository repositories)
    : ICommandHandler<UpdateRepositoryCommand, Repository>
{
    private const string DuplicateNameError = "you already have one repository with this name";

    public async Task<Repository> HandleAsync(UpdateRepositoryCommand command, CancellationToken cancellationToken = default)
    {
        var repository = await repositories.GetByIdAsync(command.Id, command.OwnerId, cancellationToken)
            ?? throw AppException.BadRequest("Repository not found");

        var duplicate = await repositories.GetByNameAsync(command.Name, command.OwnerId, cancellationToken: cancellationToken);

        if (duplicate is not null && duplicate.Id != command.Id)
        {
            throw AppException.BadRequest(DuplicateNameError);
        }

        repository.Update(command.Name, command.Description, command.Visibility, command.Language);

        await repositories.UpdateAsync(repository, cancellationToken);

        return repository;
    }
}
