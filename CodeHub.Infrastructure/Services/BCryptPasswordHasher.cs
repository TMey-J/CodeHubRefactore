namespace CodeHub.Infrastructure.Services;

using System.Security.Cryptography;
using System.Text;
using CodeHub.Application.Abstractions;

/// <summary>
/// Hashes passwords exactly like the legacy service:
/// SHA-256 (to lift the 72-byte bcrypt limit) → base64 → bcrypt.
/// Existing database hashes keep verifying.
/// </summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(PreHash(password));

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(PreHash(password), hash);

    private static string PreHash(string password)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(digest);
    }
}
