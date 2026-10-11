namespace CodeHub.Application.Features.File;

using System.Text;
using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;
using CodeHub.Domain.Entities;

public sealed record ListRepositoryFilesQuery(int RepositoryId)
    : IQuery<IReadOnlyList<RepositoryFile>>;

public sealed class ListRepositoryFilesHandler(IRepositoryRepository repositories, IFileRepository files)
    : IQueryHandler<ListRepositoryFilesQuery, IReadOnlyList<RepositoryFile>>
{
    public async Task<IReadOnlyList<RepositoryFile>> HandleAsync(ListRepositoryFilesQuery query, CancellationToken cancellationToken = default)
    {
        _ = await repositories.GetByIdAsync(query.RepositoryId, cancellationToken: cancellationToken)
            ?? throw AppException.NotFound("Repository not found");

        return await files.GetAllAsync(query.RepositoryId, cancellationToken);
    }
}

public sealed record GetFileContentQuery(int RepositoryId, int FileId)
    : IQuery<FileContentData>;

public sealed record FileContentData(
    int Id,
    string FileName,
    string RelativePath,
    string? Content,
    bool IsBinary,
    long FileSize);

public sealed class GetFileContentHandler(IRepositoryRepository repositories, IFileRepository files, IFileStorage storage)
    : IQueryHandler<GetFileContentQuery, FileContentData>
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public async Task<FileContentData> HandleAsync(GetFileContentQuery query, CancellationToken cancellationToken = default)
    {
        _ = await repositories.GetByIdAsync(query.RepositoryId, cancellationToken: cancellationToken)
            ?? throw AppException.BadRequest("Repository not found");

        var file = await files.GetByIdAsync(query.FileId, cancellationToken)
            ?? throw AppException.BadRequest("File not found");

        if (file.RepositoryId != query.RepositoryId)
        {
            throw AppException.BadRequest("File does not belong to repository");
        }

        if (!storage.Exists(file.FilePath))
        {
            throw AppException.BadRequest("Physical file not found");
        }

        if (!file.FileName.IsTextFile())
        {
            return new FileContentData(file.Id, file.FileName, file.RelativePath, null, true, file.FileSize);
        }

        var bytes = await storage.ReadAsync(file.FilePath, cancellationToken);

        string content;
        try
        {
            content = StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            throw AppException.BadRequest("Binary files are not supported");
        }

        return new FileContentData(file.Id, file.FileName, file.RelativePath, content, false, file.FileSize);
    }
}

public sealed record DownloadFileQuery(int RepositoryId, int FileId)
    : IQuery<RepositoryFile>;

public sealed class DownloadFileHandler(IRepositoryRepository repositories, IFileRepository files, IFileStorage storage)
    : IQueryHandler<DownloadFileQuery, RepositoryFile>
{
    public async Task<RepositoryFile> HandleAsync(DownloadFileQuery query, CancellationToken cancellationToken = default)
    {
        _ = await repositories.GetByIdAsync(query.RepositoryId, cancellationToken: cancellationToken)
            ?? throw AppException.NotFound("Repository not found");

        var file = await files.GetByIdAsync(query.FileId, cancellationToken)
            ?? throw AppException.NotFound("File not found");

        if (file.RepositoryId != query.RepositoryId)
        {
            throw AppException.NotFound("File does not belong to repository");
        }

        if (!storage.Exists(file.FilePath))
        {
            throw AppException.NotFound("Physical file not found");
        }

        return file;
    }
}

public sealed record DownloadRepositoryQuery(int RepositoryId)
    : IQuery<string>;

public sealed class DownloadRepositoryHandler(IRepositoryRepository repositories, IFileStorage storage)
    : IQueryHandler<DownloadRepositoryQuery, string>
{
    public async Task<string> HandleAsync(DownloadRepositoryQuery query, CancellationToken cancellationToken = default)
    {
        var repository = await repositories.GetByIdAsync(query.RepositoryId, cancellationToken: cancellationToken)
            ?? throw AppException.NotFound("Repository not found");

        if (!storage.RepositoryDirectoryExists(repository.Id))
        {
            throw AppException.NotFound("Repository files not found");
        }

        return await storage.CreateZipArchiveAsync(repository.Id, repository.Name);
    }
}
