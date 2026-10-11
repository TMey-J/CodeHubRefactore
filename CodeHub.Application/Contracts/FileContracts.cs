namespace CodeHub.Application.Contracts;

public sealed record FileResponse(
    int Id,
    int RepositoryId,
    string FileName,
    long FileSize,
    string UploadedAt,
    string RelativePath);

public sealed record FileContentResponse(
    int Id,
    string FileName,
    string RelativePath,
    string? Content,
    bool IsBinary,
    string? DownloadUrl,
    long FileSize);

public sealed record AnalyzeFileRequest(int FileId, bool EnResponse);

public sealed record VulnerabilityFileResponse(int FileId, string Content);

public sealed record OptimizationFileRequest(int FileId);

public sealed record OptimizationFileResponse(int FileId, string Content);

public sealed record ChangeFileContentRequest(int FileId, string Content);
