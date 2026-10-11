namespace CodeHub.Application.Contracts;

using System.Text.RegularExpressions;
using CodeHub.Application.Common;
using CodeHub.Domain.Enums;

public sealed record CreateRepositoryRequest(
    string Name,
    string? Description,
    RepositoryVisibility Visibility,
    string Language);

public sealed class CreateRepositoryRequestValidator : IValidator<CreateRepositoryRequest>
{
    public IReadOnlyList<string> Validate(CreateRepositoryRequest instance)
    {
        var errors = new List<string>();
        RepositoryRequestRules.ValidateName(instance.Name, errors);
        RepositoryRequestRules.ValidateLanguage(instance.Language, errors);

        if (instance.Description is { Length: > 500 })
        {
            errors.Add("Description must be at most 500 characters");
        }

        return errors;
    }
}

public sealed record UpdateRepositoryRequest(
    int Id,
    string Name,
    string? Description,
    RepositoryVisibility Visibility,
    string Language);

public sealed class UpdateRepositoryRequestValidator : IValidator<UpdateRepositoryRequest>
{
    public IReadOnlyList<string> Validate(UpdateRepositoryRequest instance)
    {
        var errors = new List<string>();
        RepositoryRequestRules.ValidateName(instance.Name, errors);
        RepositoryRequestRules.ValidateLanguage(instance.Language, errors);

        if (instance.Description is { Length: > 500 })
        {
            errors.Add("Description must be at most 500 characters");
        }

        return errors;
    }
}

public sealed record DeleteRepositoryRequest(int Id);

public sealed record SearchRepositoriesRequest(string Query);

public sealed class SearchRepositoriesRequestValidator : IValidator<SearchRepositoriesRequest>
{
    public IReadOnlyList<string> Validate(SearchRepositoriesRequest instance) =>
        string.IsNullOrWhiteSpace(instance.Query)
            ? ["Query is required"]
            : [];
}

public sealed record GenerateReadmeRequest(int RepositoryId, bool EnResponse);

public sealed record GenerateReadmeResponse(string? Content, string? FilePath);

public sealed record ToggleStarResponse(bool Starred, int StarsCount);

public sealed record RepositoryResponse(
    int Id,
    string Name,
    string? Description,
    RepositoryVisibility Visibility,
    string Language,
    int OwnerId,
    string OwnerName,
    string CreatedAt,
    string UpdatedAt,
    int StarsCount);

internal static class RepositoryRequestRules
{
    public static void ValidateName(string? name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
        {
            errors.Add("Repository name must be 1-100 characters");
            return;
        }

        if (!Regex.IsMatch(name, "^[a-zA-Z0-9_-]+$"))
        {
            errors.Add("Repository name can only contain letters, numbers, hyphens, and underscores");
        }

        if (name.StartsWith('-') || name.StartsWith('_'))
        {
            errors.Add("Repository name cannot start with hyphen or underscore");
        }

        if (name.EndsWith('-') || name.EndsWith('_'))
        {
            errors.Add("Repository name cannot end with hyphen or underscore");
        }
    }

    public static void ValidateLanguage(string? language, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            errors.Add("Language name is required");
            return;
        }

        if (language.Length > 50 || !Regex.IsMatch(language, "^[a-zA-Z0-9+#\\s-]+$"))
        {
            errors.Add("Language name contains invalid characters");
        }
    }
}
