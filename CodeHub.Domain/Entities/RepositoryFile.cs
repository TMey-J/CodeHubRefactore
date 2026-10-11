namespace CodeHub.Domain.Entities;

using CodeHub.Domain.Common;

public sealed class RepositoryFile : Entity
{
    public int RepositoryId { get; private set; }
    public string FileName { get; private set; } = null!;
    public string StoredName { get; private set; } = null!;
    public string FilePath { get; private set; } = null!;
    public long FileSize { get; private set; }
    public string RelativePath { get; private set; } = null!;
    public DateTime UploadedAt { get; private set; }

    public Repository Repository { get; private set; } = null!;

    private RepositoryFile()
    {
    }

    public static RepositoryFile Create(
        int repositoryId,
        string fileName,
        string relativePath,
        string storedName,
        string filePath,
        long fileSize) => new()
    {
        RepositoryId = repositoryId,
        FileName = fileName,
        RelativePath = relativePath,
        StoredName = storedName,
        FilePath = filePath,
        FileSize = fileSize,
        UploadedAt = Clock.UtcNow
    };

    public void ReplaceContent(string storedName, string filePath, long fileSize)
    {
        StoredName = storedName;
        FilePath = filePath;
        FileSize = fileSize;
        UploadedAt = Clock.UtcNow;
    }
}
