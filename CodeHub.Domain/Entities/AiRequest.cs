namespace CodeHub.Domain.Entities;

using CodeHub.Domain.Common;

public sealed class AiRequest : Entity
{
    /// <summary>Null when the request is issued by an anonymous caller.</summary>
    public int? UserId { get; private set; }
    public string Service { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    public User? User { get; private set; }

    private AiRequest()
    {
    }

    public static AiRequest Create(int? userId, string service) => new()
    {
        UserId = userId,
        Service = service,
        CreatedAt = Clock.UtcNow
    };
}
