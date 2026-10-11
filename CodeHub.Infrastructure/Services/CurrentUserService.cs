namespace CodeHub.Infrastructure.Services;

using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;
using CodeHub.Domain.Entities;
using Microsoft.AspNetCore.Http;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor, IUserRepository users)
    : ICurrentUser
{
    public async Task<User> GetUserAsync(CancellationToken cancellationToken = default)
    {
        var username = httpContextAccessor.HttpContext?.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
            ?? throw AppException.Unauthorized("token invalid");

        return await users.GetByUsernameAsync(username, cancellationToken)
            ?? throw AppException.Unauthorized("token invalid");
    }
}
