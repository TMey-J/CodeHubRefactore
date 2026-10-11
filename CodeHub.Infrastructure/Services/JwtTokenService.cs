namespace CodeHub.Infrastructure.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CodeHub.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; init; } = string.Empty;
    public string Algorithm { get; init; } = "HS256";
    public int AccessTokenExpirationMinutes { get; init; } = 30;
    public int RefreshTokenExpirationDays { get; init; } = 7;
}

/// <summary>
/// Issues HS256 tokens carrying the legacy claims: sub (username) and type (access|refresh).
/// </summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private const string TypeClaim = "type";
    private const string AccessType = "access";
    private const string RefreshType = "refresh";

    private readonly JwtOptions _options = options.Value;

    public TokenPair CreateTokens(string username) => new(
        AccessToken: CreateToken(username, AccessType, TimeSpan.FromMinutes(_options.AccessTokenExpirationMinutes)),
        RefreshToken: CreateToken(username, RefreshType, TimeSpan.FromDays(_options.RefreshTokenExpirationDays)));

    private string CreateToken(string username, string tokenType, TimeSpan lifetime)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(TypeClaim, tokenType)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
            _options.Algorithm);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
