namespace CodeHub.Application.Features.File;

using System.Text;
using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;
using CodeHub.Domain.Entities;

public sealed record FileUpload(string FileName, string RelativePath, byte[] Content);

public sealed record UploadFilesCommand(
    int OwnerId,
    int RepositoryId,
    IReadOnlyList<FileUpload> Files)
    : ICommand;

public sealed class UploadFilesHandler(
    IRepositoryRepository repositories,
    IFileRepository files,
    IFileStorage storage)
    : ICommandHandler<UploadFilesCommand>
{
    public async Task HandleAsync(UploadFilesCommand command, CancellationToken cancellationToken = default)
    {
        var repository = await repositories.GetByIdAsync(command.RepositoryId, command.OwnerId, cancellationToken)
            ?? throw AppException.BadRequest("Repository not found");

        var createdNewFile = false;

        foreach (var upload in command.Files)
        {
            var relativePath = storage.NormalizeRelativePath(upload.RelativePath);

            var pathFileName = Path.GetFileName(upload.RelativePath.Replace('\\', '/'));
            if (!string.Equals(upload.FileName, pathFileName, StringComparison.Ordinal))
            {
                throw AppException.BadRequest($"Filename mismatch: {upload.FileName} != {upload.RelativePath}");
            }

            var stored = await storage.SaveRepositoryFileAsync(command.RepositoryId, relativePath, upload.Content, cancellationToken);

            var existing = await files.GetByPathAsync(command.RepositoryId, relativePath, cancellationToken);

            if (existing is not null)
            {
                existing.ReplaceContent(stored.StoredName, stored.FullPath, upload.Content.LongLength);
                await files.UpdateAsync(existing, cancellationToken);
            }
            else
            {
                var file = RepositoryFile.Create(
                    command.RepositoryId,
                    upload.FileName,
                    relativePath,
                    stored.StoredName,
                    stored.FullPath,
                    upload.Content.LongLength);

                await files.AddAsync(file, cancellationToken);
                createdNewFile = true;
            }
        }

        if (createdNewFile)
        {
            repository.MarkUpdated();
            await repositories.UpdateAsync(repository, cancellationToken);
        }
    }
}

public sealed record ImportZipCommand(
    int OwnerId,
    int RepositoryId,
    string FileName,
    byte[] Content)
    : ICommand;

public sealed class ImportZipHandler(
    IRepositoryRepository repositories,
    IFileRepository files,
    IFileStorage storage)
    : ICommandHandler<ImportZipCommand>
{
    public async Task HandleAsync(ImportZipCommand command, CancellationToken cancellationToken = default)
    {
        var repository = await repositories.GetByIdAsync(command.RepositoryId, command.OwnerId, cancellationToken)
            ?? throw AppException.BadRequest("Repository not found");

        if (!command.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.BadRequest("Only zip file allowed");
        }

        if (storage.RepositoryDirectoryExists(command.RepositoryId))
        {
            await storage.DeleteRepositoryDirectoryAsync(command.RepositoryId);
            await files.RemoveAllForRepositoryAsync(command.RepositoryId, cancellationToken);
        }

        var entries = await storage.ExtractZipAsync(command.RepositoryId, command.Content, cancellationToken);

        foreach (var entry in entries)
        {
            var file = RepositoryFile.Create(
                command.RepositoryId,
                entry.FileName,
                entry.RelativePath,
                entry.FileName,
                entry.FullPath,
                entry.Size);

            await files.AddAsync(file, cancellationToken);
        }

        repository.MarkUpdated();
        await repositories.UpdateAsync(repository, cancellationToken);
    }
}

public sealed record DeleteFileCommand(int OwnerId, int RepositoryId, int FileId)
    : ICommand<bool>;

public sealed class DeleteFileHandler(
    IRepositoryRepository repositories,
    IFileRepository files,
    IFileStorage storage)
    : ICommandHandler<DeleteFileCommand, bool>
{
    public async Task<bool> HandleAsync(DeleteFileCommand command, CancellationToken cancellationToken = default)
    {
        var repository = await repositories.GetByIdAsync(command.RepositoryId, command.OwnerId, cancellationToken)
            ?? throw AppException.NotFound("Repository not found");

        var file = await files.GetByIdAsync(command.FileId, cancellationToken)
            ?? throw AppException.NotFound("File not found");

        if (file.RepositoryId != repository.Id)
        {
            throw AppException.NotFound("File does not belong to repository");
        }

        await storage.DeleteAsync(file.FilePath);

        await files.RemoveAsync(file, cancellationToken);

        repository.MarkUpdated();
        await repositories.UpdateAsync(repository, cancellationToken);

        return true;
    }
}

public sealed record ChangeFileContentCommand(int OwnerId, int FileId, string Content)
    : ICommand;

public sealed class ChangeFileContentHandler(
    IFileRepository files,
    IRepositoryRepository repositories,
    IFileStorage storage)
    : ICommandHandler<ChangeFileContentCommand>
{
    public async Task HandleAsync(ChangeFileContentCommand command, CancellationToken cancellationToken = default)
    {
        var file = await files.GetByIdAsync(command.FileId, cancellationToken)
            ?? throw AppException.BadRequest("File not found");

        var isOwner = await repositories.ExistsAsync(file.RepositoryId, command.OwnerId, cancellationToken);

        if (!isOwner)
        {
            throw AppException.BadRequest("File not found");
        }

        await storage.WriteTextAsync(file.FilePath, command.Content);

        // The legacy API stored the character count for edited files, not the byte count.
        file.ReplaceContent(file.StoredName, file.FilePath, command.Content.Length);

        await files.UpdateAsync(file, cancellationToken);
    }
}

public sealed record AnalyzeVulnerabilityCommand(int UserId, int FileId, bool English)
    : ICommand<string>;

public sealed class AnalyzeVulnerabilityHandler(
    IFileRepository files,
    IFileStorage storage,
    IAiClient aiClient,
    IAiRequestRepository aiRequests)
    : ICommandHandler<AnalyzeVulnerabilityCommand, string>
{
    public async Task<string> HandleAsync(AnalyzeVulnerabilityCommand command, CancellationToken cancellationToken = default)
    {
        var file = await files.GetByIdAsync(command.FileId, cancellationToken)
            ?? throw AppException.BadRequest("File not found");

        if (!file.RelativePath.IsTextFile())
        {
            throw AppException.BadRequest("File must be a text file");
        }

        var content = await storage.ReadAsync(file.FilePath, cancellationToken);

        await aiRequests.AddAsync(command.UserId, AiServiceNames.Vulnerability, cancellationToken);

        var code = DecodeLenient(content);

        return await aiClient.AnalyzeVulnerabilityAsync(code, command.English, cancellationToken);
    }

    internal static string DecodeLenient(byte[] bytes) => Encoding.UTF8.GetString(bytes);
}

public sealed record OptimizeFileCommand(int UserId, int FileId)
    : ICommand<string>;

public sealed class OptimizeFileHandler(
    IFileRepository files,
    IRepositoryRepository repositories,
    IFileStorage storage,
    IAiClient aiClient,
    IAiRequestRepository aiRequests)
    : ICommandHandler<OptimizeFileCommand, string>
{
    public async Task<string> HandleAsync(OptimizeFileCommand command, CancellationToken cancellationToken = default)
    {
        var file = await files.GetByIdAsync(command.FileId, cancellationToken)
            ?? throw AppException.BadRequest("File not found");

        var isOwner = await repositories.ExistsAsync(file.RepositoryId, command.UserId, cancellationToken);

        if (!isOwner)
        {
            throw AppException.BadRequest("File not found");
        }

        if (!file.RelativePath.IsTextFile())
        {
            throw AppException.BadRequest("File must be a text file");
        }

        var content = await storage.ReadAsync(file.FilePath, cancellationToken);

        await aiRequests.AddAsync(command.UserId, AiServiceNames.Optimization, cancellationToken);

        var code = Encoding.UTF8.GetString(content);

        return await aiClient.OptimizeCodeAsync(code, cancellationToken);
    }
}
