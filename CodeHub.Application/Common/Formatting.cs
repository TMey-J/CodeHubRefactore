namespace CodeHub.Application.Common;

using System.Text.RegularExpressions;

/// <summary>
/// Names of the AI services as recorded in the ai_requests table.
/// </summary>
public static class AiServiceNames
{
    public const string Vulnerability = "vulnerability";
    public const string Optimization = "optimization";
    public const string Readme = "readme";
    public const string RepositorySearch = "repository_search";
}

/// <summary>
/// File extensions that are treated as readable text (port of the legacy TEXT_EXTENSIONS list).
/// </summary>
public static class TextFileExtensions
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".py", ".cs", ".js", ".ts", ".html", ".css",
        ".json", ".xml", ".yaml", ".yml", ".sql", ".sh", ".bat",
        ".dockerfile", ".gitignore", ".env",
        ".cpp", ".c", ".h", ".hpp",
        ".java",
        ".rb", ".go", ".rs",
        ".php", ".asp", ".aspx",
        ".log", ".cfg", ".conf", ".ini",
        ".csv", ".tsv",
        ".rst", ".tex",
        ".vue", ".jsx", ".tsx",
        ".rbxlx", ".lua",
        ".toml"
    };

    public static bool IsTextFile(this string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return Extensions.Contains(extension);
    }
}

public static class DateTimeFormatting
{
    public const string DisplayFormat = "yyyy-MM-dd | HH:mm:ss";

    /// <summary>
    /// Legacy display format: "YYYY-MM-DD | HH:MM:SS".
    /// </summary>
    public static string? ToDisplayString(this DateTime? value) =>
        value?.ToString(DisplayFormat);

    public static string ToDisplayString(this DateTime value) =>
        value.ToString(DisplayFormat);
}
