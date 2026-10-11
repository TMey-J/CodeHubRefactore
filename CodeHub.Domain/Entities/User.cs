namespace CodeHub.Domain.Entities;

using CodeHub.Domain.Common;

public sealed class User : Entity
{
    public string Username { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string HashedPassword { get; private set; } = null!;

    // The users table stores naive (timezone-less) timestamps,
    // hence the Unspecified kind when a user is created.
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private User()
    {
    }

    public static User Create(string username, string email, string hashedPassword)
    {
        var now = DateTime.SpecifyKind(Clock.UtcNow, DateTimeKind.Unspecified);

        return new User
        {
            Username = username,
            Email = email,
            HashedPassword = hashedPassword,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
