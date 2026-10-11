namespace CodeHub.Application.Common;

/// <summary>
/// The standard envelope returned by every endpoint
/// (kept wire-compatible with the legacy client contract: is_success / errors / response).
/// </summary>
public sealed record ApiResponse<T>
{
    public bool IsSuccess { get; private init; }
    public List<string> Errors { get; private init; } = [];
    public T? Response { get; private init; }

    public static ApiResponse<T> Ok(T response) => new()
    {
        IsSuccess = true,
        Errors = [],
        Response = response
    };

    public static ApiResponse<T> Ok() => new()
    {
        IsSuccess = true,
        Errors = [],
        Response = default
    };

    public static ApiResponse<T> Fail(IReadOnlyList<string> errors) => new()
    {
        IsSuccess = false,
        Errors = [.. errors],
        Response = default
    };
}

/// <summary>
/// A page of items with pagination metadata.
/// </summary>
public sealed record PagedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int Take { get; init; }
    public int TotalPages { get; private init; }

    public static PagedResponse<T> Create(IReadOnlyList<T> items, int totalCount, int page, int take) => new()
    {
        Items = items,
        TotalCount = totalCount,
        Page = page,
        Take = take,
        TotalPages = take <= 0 ? 0 : (totalCount + take - 1) / take
    };
}

/// <summary>
/// Result of a paged repository read, before DTO mapping.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount);
