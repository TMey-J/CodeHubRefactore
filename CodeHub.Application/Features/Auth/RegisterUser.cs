namespace CodeHub.Application.Features.Auth;

using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;
using CodeHub.Application.Common.Mapping;
using CodeHub.Application.Contracts;
using CodeHub.Domain.Entities;
using CodeHub.Domain.ValueObjects;

public sealed record RegisterUserCommand(string Username, string Email, string Password)
    : ICommand<UserResponse>;

public sealed class RegisterUserHandler(IUserRepository users, IPasswordHasher passwordHasher)
    : ICommandHandler<RegisterUserCommand, UserResponse>
{
    public async Task<UserResponse> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        if (!Username.TryCreate(command.Username, out var username))
        {
            throw AppException.BadRequest(Username.ValidationError);
        }

        if (!Email.TryCreate(command.Email, out var email))
        {
            throw AppException.BadRequest(Email.ValidationError);
        }

        if (!Password.TryCreate(command.Password, out var password))
        {
            throw AppException.BadRequest(Password.ValidationError);
        }

        if (await users.GetByUsernameAsync(username.Value, cancellationToken) is not null)
        {
            throw AppException.BadRequest("Username already exists");
        }

        if (await users.GetByEmailAsync(email.Value, cancellationToken) is not null)
        {
            throw AppException.BadRequest("Email already exists");
        }

        var user = User.Create(username.Value, email.Value, passwordHasher.Hash(password.Value));

        await users.AddAsync(user, cancellationToken);

        return user.ToResponse();
    }
}
