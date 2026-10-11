namespace CodeHub.Domain.Common;

/// <summary>
/// Single place where domain timestamps are produced.
/// All timestamps are UTC; the persistence layer decides the column kind.
/// </summary>
public static class Clock
{
    public static DateTime UtcNow => DateTime.UtcNow;
}
