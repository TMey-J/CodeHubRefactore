namespace CodeHub.Infrastructure.Services;

using System.IO.Compression;
using System.Security.Cryptography;
using CodeHub.Application.Abstractions;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Filesystem backed storage rooted at "storage/repositories" (configurable),
/// plus zip extraction/archiving for repository import and download.
/// </summary>
public sealed class LocalFileStorage(IHostEnvironment environment) : IFileStorage
{
    private const string RepositoriesRoot = "storage/repositories";
    private const string TempRoot = "tmp";

    private string RootPath => Path.Combine(environment.ContentRootPath, RepositoriesRoot);
    private string TempPath => Path.Combine(environment.ContentRootPath, TempRoot);

    public string NormalizeRelativePath(string relativePath)
    {
        var withForwardSlashes = relativePath.Replace('\\', '/').TrimStart('/');

        if (withForwardSlashes.Length == 0)
        {
            throw new CodeHub.Application.Common.AppException("Invalid path", 400);
        }

        // Reject traversal attempts such as "../secret" or "a/../../b".
        var candidate = Path.GetFullPath(Path.Combine("/", withForwardSlashes));
        var rooted = Path.TrimEndingDirectorySeparator(candidate).Replace('\\', '/').TrimStart('/');

        var segments = rooted.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(s => s == ".."))
        {
            throw new CodeHub.Application.Common.AppException("Invalid path", 400);
        }

        return string.Join('/', segments);
    }

    public async Task<StoredFile> SaveRepositoryFileAsync(
        int repositoryId,
        string relativePath,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(RootPath, repositoryId.ToString(), relativePath.Replace('/', Path.DirectorySeparatorChar));

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, content, cancellationToken);

        return new StoredFile(Guid.NewGuid().ToString(), fullPath);
    }

    public Task DeleteAsync(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public bool Exists(string path) => File.Exists(path);

    public async Task<byte[]> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"File not found: {path}", path);
        }

        return await File.ReadAllBytesAsync(path, cancellationToken);
    }

    public async Task WriteTextAsync(string path, string content)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"File not found: {path}", path);
        }

        await File.WriteAllTextAsync(path, content);
    }

    public Task<StoredFile> SaveReadmeAsync(int repositoryId, string content)
    {
        var repositoryPath = Path.Combine(RootPath, repositoryId.ToString());
        Directory.CreateDirectory(repositoryPath);

        var readmePath = Path.Combine(repositoryPath, "README.md");
        File.WriteAllText(readmePath, content);

        return Task.FromResult(new StoredFile("README.md", readmePath));
    }

    public Task<IReadOnlyList<ExtractedZipEntry>> ExtractZipAsync(
        int repositoryId,
        byte[] zipContent,
        CancellationToken cancellationToken = default)
    {
        var extractPath = Path.Combine(RootPath, repositoryId.ToString());
        Directory.CreateDirectory(extractPath);

        var entries = new List<ExtractedZipEntry>();

        using var archive = new ZipArchive(new MemoryStream(zipContent), ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.EndsWith('/') || string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            var relativePath = NormalizeRelativePath(entry.FullName);
            var fullPath = Path.Combine(extractPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            using var entryStream = entry.Open();
            using var fileStream = File.Create(fullPath);
            entryStream.CopyTo(fileStream);

            var size = entry.Length;
            if (fileStream.Length != size)
            {
                size = fileStream.Length;
            }

            entries.Add(new ExtractedZipEntry(relativePath, Path.GetFileName(relativePath), fullPath, size));
        }

        return Task.FromResult<IReadOnlyList<ExtractedZipEntry>>(entries);
    }

    public Task DeleteRepositoryDirectoryAsync(int repositoryId)
    {
        var path = Path.Combine(RootPath, repositoryId.ToString());

        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        return Task.CompletedTask;
    }

    public bool RepositoryDirectoryExists(int repositoryId) =>
        Directory.Exists(Path.Combine(RootPath, repositoryId.ToString()));

    public Task<string> CreateZipArchiveAsync(int repositoryId, string repositoryName)
    {
        var repositoryPath = Path.Combine(RootPath, repositoryId.ToString());

        if (!Directory.Exists(repositoryPath))
        {
            throw new DirectoryNotFoundException($"Repository files not found: {repositoryPath}");
        }

        Directory.CreateDirectory(TempPath);

        var zipPath = Path.Combine(TempPath, $"repo_{repositoryName}.zip");

        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);

        foreach (var filePath in Directory.EnumerateFiles(repositoryPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(repositoryPath, filePath);
            archive.CreateEntryFromFile(filePath, relativePath, CompressionLevel.Optimal);
        }

        return Task.FromResult(zipPath);
    }
}
