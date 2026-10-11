namespace CodeHub.Application.Features.Auth;

using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;
using CodeHub.Application.Contracts;

public sealed record LoginUserCommand(string Username, string Password)
    : ICommand<TokenResponse>;

public sealed class LoginUserHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokenService)
    : ICommandHandler<LoginUserCommand, TokenResponse>
{
    private const string TokenType = "bearer";

    public async Task<TokenResponse> HandleAsync(LoginUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByUsernameAsync(command.Username, cancellationToken);

        if (user is null || !passwordHasher.Verify(command.Password, user.HashedPassword))
        {
            throw AppException.Unauthorized("Invalid username or password");
        }

        var tokens = tokenService.CreateTokens(user.Username);

        return new TokenResponse(tokens.AccessToken, tokens.RefreshToken, TokenType);
    }
}
