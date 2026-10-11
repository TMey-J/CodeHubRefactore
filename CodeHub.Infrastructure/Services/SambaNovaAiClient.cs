namespace CodeHub.Infrastructure.Services;

using System.Net.Http.Json;
using System.Text.Json;
using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;
using Microsoft.Extensions.Options;

public sealed class SambaNovaOptions
{
    public const string SectionName = "SambaNova";

    public string BaseUrl { get; init; } = "https://api.sambanova.ai/v1";
    public string Model { get; init; } = "gpt-oss-120b";
    public string[] ApiKeys { get; init; } = [];
}

/// <summary>
/// SambaNova (OpenAI-compatible) chat client.
/// Rotates through the configured keys when one hits its daily rate limit;
/// throws <see cref="SambaNovaDailyLimitException"/> once all keys are exhausted.
/// </summary>
public sealed class SambaNovaAiClient : IAiClient
{
    private readonly HttpClient _httpClient;
    private readonly SambaNovaOptions _options;
    private int _keyIndex;

    public SambaNovaAiClient(HttpClient httpClient, IOptions<SambaNovaOptions> options)
    {
        _options = options.Value;
        _httpClient = httpClient;
    }

    public async Task<string> AnalyzeVulnerabilityAsync(string code, bool english, CancellationToken cancellationToken = default)
    {
        const string SystemPrompt = """
        You are an expert DevSecOps analyzer.

        Return ONLY valid JSON.

        {
        "scores":{
        "readability":1,
        "security":1,
        "performance":1,
        "maintainability":1
        },
        "overall_rating":"",
        "critical_vulnerabilities":[],
        "actionable_feedback":""
        }
        """;

        var prompt = english ? SystemPrompt : SystemPrompt.Replace("valid JSON", "valid JSON in Persian");
        return CleanJson(await ChatAsync(prompt, code, temperature: 0.1, topP: 0.1, cancellationToken));
    }

    public async Task<string> OptimizeCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        const string SystemPrompt = """
        You are an elite software architect.

        Return ONLY optimized code.

        No markdown.
        No explanations.
        """;

        return CleanMarkdown(await ChatAsync(SystemPrompt, code, temperature: 0.1, topP: 0.1, cancellationToken));
    }

    public async Task<string> GenerateReadmeAsync(string projectInfo, bool english, CancellationToken cancellationToken = default)
    {
        var systemPrompt = english
            ? """
            Generate a professional README.md.

            Return ONLY markdown.
            """
            : """
            Generate a professional README.md in Persian.

            Return ONLY markdown.
            """;

        return (await ChatAsync(systemPrompt, projectInfo, temperature: 0.3, topP: 0.8, cancellationToken)).Trim();
    }

    public async Task<IReadOnlyList<string>> SelectReadmeFilesAsync(
        IReadOnlyList<ReadmeCandidateFile> candidates,
        CancellationToken cancellationToken = default)
    {
        const string SystemPrompt = """
        Choose only important files.

        Return JSON only.

        {
        "files":[]
        }
        """;

        var userPrompt = string.Join("\n", candidates.Select(c => $"{c.Path} ({c.Lines} lines)"));

        var response = CleanJson(await ChatAsync(SystemPrompt, userPrompt, temperature: 0.1, topP: 0.1, cancellationToken));

        return ParseStringArray(response, "files");
    }

    public async Task<IReadOnlyList<int>> SearchRepositoriesAsync(
        string query,
        IReadOnlyList<RepositorySearchItem> repositories,
        CancellationToken cancellationToken = default)
    {
        const string SystemPrompt = """
        You are a repository search engine.

        Return only JSON.
        """;

        var repositoriesJson = JsonSerializer.Serialize(repositories);
        var userPrompt = $$"""
        User Query:

        {{query}}

        Repositories:

        {{repositoriesJson}}

        Return ONLY JSON

        {
        "repository_ids":[]
        }
        """;

        var response = CleanJson(await ChatAsync(SystemPrompt, userPrompt, temperature: 0, topP: 0.1, cancellationToken));

        return ParseIntArray(response, "repository_ids");
    }

    private async Task<string> ChatAsync(string systemPrompt, string userPrompt, double temperature, double topP, CancellationToken cancellationToken)
    {
        if (_options.ApiKeys.Length == 0)
        {
            throw new AppException("No SambaNova API Keys found.", 500);
        }

        var startIndex = Volatile.Read(ref _keyIndex);

        while (true)
        {
            try
            {
                return await SendChatAsync(systemPrompt, userPrompt, temperature, topP, cancellationToken);
            }
            catch (SambaNovaRateLimitException)
            {
                var nextIndex = (Volatile.Read(ref _keyIndex) + 1) % _options.ApiKeys.Length;
                Volatile.Write(ref _keyIndex, nextIndex);

                if (nextIndex == startIndex)
                {
                    throw new SambaNovaDailyLimitException();
                }
            }
        }
    }

    private async Task<string> SendChatAsync(string systemPrompt, string userPrompt, double temperature, double topP, CancellationToken cancellationToken)
    {
        var request = new
        {
            model = _options.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature,
            top_p = topP
        };

        var apiKey = _options.ApiKeys[Volatile.Read(ref _keyIndex)];

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions");
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = JsonContent.Create(request);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            if ((int)response.StatusCode == 429 || IsDailyLimitBody(body))
            {
                throw new SambaNovaRateLimitException();
            }

            throw new AppException($"AI service error ({(int)response.StatusCode}).", 500);
        }

        using var document = JsonDocument.Parse(body);
        return document.RootElement
                   .GetProperty("choices")[0]
                   .GetProperty("message")
                   .GetProperty("content")
                   .GetString()
               ?? string.Empty;
    }

    private static bool IsDailyLimitBody(string body) =>
        body.Contains("rate_limit_daily", StringComparison.OrdinalIgnoreCase) ||
        body.Contains("1-day token limit", StringComparison.OrdinalIgnoreCase);

    private static string CleanJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');

        if (start != -1 && end != -1 && end > start)
        {
            return text[start..(end + 1)];
        }

        return CleanMarkdown(text);
    }

    private static string CleanMarkdown(string text)
    {
        var trimmed = text.Trim();
        var withoutOpeningFence = StripOpeningCodeFence(trimmed);
        return StripClosingCodeFence(withoutOpeningFence);
    }

    private static string StripOpeningCodeFence(string text)
    {
        if (!text.StartsWith("```", StringComparison.Ordinal))
        {
            return text;
        }

        var newlineIndex = text.IndexOf('\n');
        return newlineIndex == -1 ? string.Empty : text[(newlineIndex + 1)..];
    }

    private static string StripClosingCodeFence(string text)
    {
        var trimmed = text.TrimEnd();
        return trimmed.EndsWith("```", StringComparison.Ordinal)
            ? trimmed[..^3].TrimEnd()
            : trimmed;
    }

    private static IReadOnlyList<string> ParseStringArray(string json, string propertyName)
    {
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty(propertyName, out var array)
            || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString()!)
            .ToList();
    }

    private static IReadOnlyList<int> ParseIntArray(string json, string propertyName)
    {
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty(propertyName, out var array)
            || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var values = new List<int>();

        foreach (var element in array.EnumerateArray())
        {
            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value))
            {
                values.Add(value);
            }
            else if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var parsed))
            {
                values.Add(parsed);
            }
        }

        return values;
    }

    private sealed class SambaNovaRateLimitException : Exception;
}
