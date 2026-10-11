namespace CodeHub.Application.Common;

/// <summary>
/// Validates an incoming request DTO before its handler runs.
/// Implementations return human readable error messages; an empty list means the request is valid.
/// </summary>
public interface IValidator<in T>
{
    IReadOnlyList<string> Validate(T instance);
}
