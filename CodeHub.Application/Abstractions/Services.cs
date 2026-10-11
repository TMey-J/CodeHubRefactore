namespace CodeHub.Application.Abstractions;

using CodeHub.Domain.Entities;

/// <summary>
/// Write-side persistence boundary. Commands call this explicitly to commit changes.
/// Registered against the write DbContext so the read and write stores can be split later.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}

public sealed record TokenPair(string AccessToken, string RefreshToken);

public interface ITokenService
{
    TokenPair CreateTokens(string username);
}

/// <summary>
/// Resolves the authenticated user for the current HTTP request.
/// </summary>
public interface ICurrentUser
{
    Task<User> GetUserAsync(CancellationToken cancellationToken = default);
}

public sealed record StoredFile(string StoredName, string FullPath);

public sealed record ExtractedZipEntry(
    string RelativePath,
    string FileName,
    string FullPath,
    long Size);

public interface IFileStorage
{
    /// <summary>Normalizes a client supplied relative path and rejects traversal attempts.</summary>
    string NormalizeRelativePath(string relativePath);

    Task<StoredFile> SaveRepositoryFileAsync(
        int repositoryId,
        string relativePath,
        byte[] content,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string path);

    bool Exists(string path);

    Task<byte[]> ReadAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes text with a strict UTF-8 decoder; throws when the bytes are not valid UTF-8.
    /// </summary>
    Task WriteTextAsync(string path, string content);

    Task<StoredFile> SaveReadmeAsync(int repositoryId, string content);

    Task<IReadOnlyList<ExtractedZipEntry>> ExtractZipAsync(
        int repositoryId,
        byte[] zipContent,
        CancellationToken cancellationToken = default);

    Task DeleteRepositoryDirectoryAsync(int repositoryId);

    bool RepositoryDirectoryExists(int repositoryId);

    /// <summary>Creates a zip archive of a repository directory and returns its full path.</summary>
    Task<string> CreateZipArchiveAsync(int repositoryId, string repositoryName);
}

public sealed record ReadmeCandidateFile(string Path, int Lines);

public sealed record RepositorySearchItem(
    int Id,
    string Name,
    string? Description,
    string Language,
    string OwnerName);

/// <summary>
/// Chat-completion based AI client. Key rotation and daily-limit detection are hidden here.
/// </summary>
public interface IAiClient
{
    /// <summary>DevSecOps analysis; returns a JSON string of scores and findings.</summary>
    Task<string> AnalyzeVulnerabilityAsync(string code, bool english, CancellationToken cancellationToken = default);

    /// <summary>Returns optimized code, plain text without markdown fences.</summary>
    Task<string> OptimizeCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Returns a full README in markdown.</summary>
    Task<string> GenerateReadmeAsync(string projectInfo, bool english, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> SelectReadmeFilesAsync(
        IReadOnlyList<ReadmeCandidateFile> candidates,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> SearchRepositoriesAsync(
        string query,
        IReadOnlyList<RepositorySearchItem> repositories,
        CancellationToken cancellationToken = default);
}
