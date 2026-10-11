namespace CodeHub.Domain.Entities;

using CodeHub.Domain.Common;

public sealed class RepositoryStar : Entity
{
    public int RepositoryId { get; private set; }
    public int UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Repository Repository { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private RepositoryStar()
    {
    }

    public static RepositoryStar Create(int repositoryId, int userId) => new()
    {
        RepositoryId = repositoryId,
        UserId = userId,
        CreatedAt = Clock.UtcNow
    };
}
