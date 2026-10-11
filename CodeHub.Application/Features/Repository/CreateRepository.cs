namespace CodeHub.Application.Features.Repository;

using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;
using CodeHub.Domain.Entities;
using CodeHub.Domain.Enums;

public sealed record CreateRepositoryCommand(
    int OwnerId,
    string Name,
    string? Description,
    RepositoryVisibility Visibility,
    string Language)
    : ICommand<Repository>;

public sealed class CreateRepositoryHandler(IRepositoryRepository repositories)
    : ICommandHandler<CreateRepositoryCommand, Repository>
{
    private const string DuplicateNameError = "you already have one repository with this name";

    public async Task<Repository> HandleAsync(CreateRepositoryCommand command, CancellationToken cancellationToken = default)
    {
        var duplicate = await repositories.GetByNameAsync(command.Name, command.OwnerId, cancellationToken: cancellationToken);

        if (duplicate is not null)
        {
            throw AppException.BadRequest(DuplicateNameError);
        }

        var repository = Repository.Create(
            command.Name,
            command.OwnerId,
            command.Visibility,
            command.Description,
            command.Language);

        return await repositories.AddAsync(repository, cancellationToken);
    }
}
