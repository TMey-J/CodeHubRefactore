namespace CodeHub.Domain.Entities;

using CodeHub.Domain.Common;
using CodeHub.Domain.Enums;

public sealed class Repository : Entity
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public RepositoryVisibility Visibility { get; private set; }
    public int OwnerId { get; private set; }
    public string Language { get; private set; } = null!;

    /// <summary>
    /// Plain-text digest (name / description / language / README) used by the AI search prompt.
    /// Kept as a column to mirror the legacy schema.
    /// </summary>
    public string SearchText { get; private set; } = string.Empty;

    public int StarsCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public User Owner { get; private set; } = null!;
    public ICollection<RepositoryFile> Files { get; private set; } = [];
    public ICollection<RepositoryStar> Stars { get; private set; } = [];

    private Repository()
    {
    }

    public static Repository Create(
        string name,
        int ownerId,
        RepositoryVisibility visibility,
        string? description,
        string language)
    {
        var repository = new Repository
        {
            Name = name,
            OwnerId = ownerId,
            Visibility = visibility,
            Description = description,
            Language = language,
            CreatedAt = Clock.UtcNow,
            UpdatedAt = Clock.UtcNow
        };

        repository.RebuildSearchText();
        return repository;
    }

    public void Update(string name, string? description, RepositoryVisibility visibility, string language)
    {
        Name = name;
        Description = description;
        Visibility = visibility;
        Language = language;
        MarkUpdated();

        RebuildSearchText();
    }

    public void MarkUpdated() => UpdatedAt = Clock.UtcNow;

    private void RebuildSearchText() =>
        SearchText = $"""
        Name:
        {Name}

        Description:
        {Description ?? string.Empty}

        Language:
        {Language}

        README:

        """;
}
