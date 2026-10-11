namespace CodeHub.Domain.Common;

/// <summary>
/// Base type for every domain entity persisted by the write side.
/// </summary>
public abstract class Entity
{
    public int Id { get; protected set; }
}
