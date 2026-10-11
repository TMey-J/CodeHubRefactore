namespace CodeHub.Application.Contracts;

using System.Text.RegularExpressions;
using CodeHub.Application.Common;
using CodeHub.Domain.ValueObjects;

public sealed record RegisterRequest(string Username, string Email, string Password);

public sealed class RegisterRequestValidator : IValidator<RegisterRequest>
{
    public IReadOnlyList<string> Validate(RegisterRequest instance)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(instance.Username) ||
            instance.Username.Length is < 3 or > 30 ||
            !Regex.IsMatch(instance.Username, "^[a-zA-Z0-9_-]+$"))
        {
            errors.Add(Username.ValidationError);
        }

        if (string.IsNullOrWhiteSpace(instance.Email) ||
            !Regex.IsMatch(instance.Email, "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"))
        {
            errors.Add("Must be a valid email address");
        }

        ValidatePassword(instance.Password, errors);
        return errors;
    }

    internal static void ValidatePassword(string? password, List<string> errors)
    {
        if (string.IsNullOrEmpty(password))
        {
            errors.Add("Password must be at least 8 characters");
            return;
        }

        if (password.Length < 8)
        {
            errors.Add("Password must be at least 8 characters");
        }

        if (!password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter");
        }

        if (!password.Any(char.IsLower))
        {
            errors.Add("Password must contain at least one lowercase letter");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one digit");
        }
    }
}

public sealed record LoginRequest(string Username, string Password);

public sealed class LoginRequestValidator : IValidator<LoginRequest>
{
    public IReadOnlyList<string> Validate(LoginRequest instance)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(instance.Username) ||
            instance.Username.Length is < 3 or > 30 ||
            !Regex.IsMatch(instance.Username, "^[a-zA-Z0-9_-]+$"))
        {
            errors.Add(Username.ValidationError);
        }

        if (string.IsNullOrEmpty(instance.Password) || instance.Password.Length < 8)
        {
            errors.Add("Password must be at least 8 characters");
        }

        return errors;
    }
}

public sealed record TokenResponse(string AccessToken, string RefreshToken, string TokenType);

public sealed record UserResponse(
    int Id,
    string Username,
    string Email,
    DateTime CreatedAt,
    DateTime UpdatedAt);
