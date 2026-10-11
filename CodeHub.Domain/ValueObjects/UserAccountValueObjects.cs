namespace CodeHub.Domain.ValueObjects;

using System.Text.RegularExpressions;

public sealed partial class Username
{
    private static readonly Regex Pattern = CreatePattern();

    public const string ValidationError =
        "Username must be 3-30 characters, alphanumeric with underscores/hyphens";

    public string Value { get; }

    private Username(string value) => Value = value;

    public static bool TryCreate(string? value, out Username username)
    {
        username = null!;

        if (string.IsNullOrWhiteSpace(value) ||
            value.Length is < 3 or > 30 ||
            !Pattern.IsMatch(value))
        {
            return false;
        }

        username = new Username(value);
        return true;
    }

    [GeneratedRegex("^[a-zA-Z0-9_-]+$")]
    private static partial Regex CreatePattern();
}

public sealed partial class Email
{
    private static readonly Regex Pattern = CreatePattern();

    public const string ValidationError = "Invalid email format";

    public string Value { get; }

    private Email(string value) => Value = value;

    public static bool TryCreate(string? value, out Email email)
    {
        email = null!;

        if (string.IsNullOrWhiteSpace(value) || !Pattern.IsMatch(value))
        {
            return false;
        }

        email = new Email(value);
        return true;
    }

    [GeneratedRegex("^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$")]
    private static partial Regex CreatePattern();
}

public sealed class Password
{
    public const string ValidationError =
        "Password must be at least 8 characters with uppercase, lowercase, and number";

    private Password(string value) => Value = value;

    public string Value { get; }

    public static bool TryCreate(string? value, out Password password)
    {
        password = null!;

        if (string.IsNullOrEmpty(value) ||
            value.Length < 8 ||
            !value.Any(char.IsUpper) ||
            !value.Any(char.IsLower) ||
            !value.Any(char.IsDigit))
        {
            return false;
        }

        password = new Password(value);
        return true;
    }
}
