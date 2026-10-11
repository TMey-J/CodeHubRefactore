namespace CodeHub.Application.Features.Repository;

using System.Text;
using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;
using CodeHub.Application.Contracts;
using CodeHub.Domain.Entities;

public sealed record GenerateReadmeCommand(int UserId, int RepositoryId, bool English)
    : ICommand<GenerateReadmeResponse>;

public sealed class GenerateReadmeHandler(
    IRepositoryRepository repositories,
    IFileRepository files,
    IFileStorage storage,
    IAiClient aiClient,
    IAiRequestRepository aiRequests)
    : ICommandHandler<GenerateReadmeCommand, GenerateReadmeResponse>
{
    private const int MaxCandidateFileSize = 50_000;

    public async Task<GenerateReadmeResponse> HandleAsync(GenerateReadmeCommand command, CancellationToken cancellationToken = default)
    {
        var repository = await repositories.GetByIdAsync(command.RepositoryId, command.UserId, cancellationToken)
            ?? throw AppException.BadRequest("Repository not found");

        var existingReadme = await files.GetByPathAsync(repository.Id, "README.md", cancellationToken);

        if (existingReadme is not null && existingReadme.UploadedAt >= repository.UpdatedAt)
        {
            throw AppException.BadRequest("Repository files not changed");
        }

        var (candidates, contentCache) = await CollectCandidateFilesAsync(repository.Id, cancellationToken);

        if (candidates.Count == 0)
        {
            throw AppException.BadRequest("No readable source files found.");
        }

        var selectedPaths = await aiClient.SelectReadmeFilesAsync(candidates, cancellationToken);

        var projectInfo = BuildProjectInfo(contentCache, selectedPaths);

        var readmeContent = command.English
            ? await aiClient.GenerateReadmeAsync(projectInfo, english: true, cancellationToken)
            : await aiClient.GenerateReadmeAsync(projectInfo, english: false, cancellationToken);

        await aiRequests.AddAsync(command.UserId, AiServiceNames.Readme, cancellationToken);

        var stored = await storage.SaveReadmeAsync(repository.Id, readmeContent);

        if (existingReadme is not null)
        {
            existingReadme.ReplaceContent(stored.StoredName, stored.FullPath, Encoding.UTF8.GetByteCount(readmeContent));
            await files.UpdateAsync(existingReadme, cancellationToken);
        }
        else
        {
            var readme = RepositoryFile.Create(
                repository.Id,
                "README.md",
                "README.md",
                stored.StoredName,
                stored.FullPath,
                Encoding.UTF8.GetByteCount(readmeContent));

            await files.AddAsync(readme, cancellationToken);
        }

        return new GenerateReadmeResponse(readmeContent, stored.FullPath);
    }

    private async Task<(IReadOnlyList<ReadmeCandidateFile> Candidates, Dictionary<string, string> ContentCache)>
        CollectCandidateFilesAsync(int repositoryId, CancellationToken cancellationToken)
    {
        var allFiles = await files.GetAllAsync(repositoryId, cancellationToken);

        var candidates = new List<ReadmeCandidateFile>();
        var contentCache = new Dictionary<string, string>();

        foreach (var file in allFiles)
        {
            if (!file.RelativePath.IsTextFile() ||
                file.RelativePath.Equals("README.md", StringComparison.OrdinalIgnoreCase) ||
                file.FileSize > MaxCandidateFileSize)
            {
                continue;
            }

            try
            {
                var content = await storage.ReadAsync(file.FilePath, cancellationToken);
                var text = Encoding.UTF8.GetString(content);

                contentCache[file.RelativePath] = text;
                candidates.Add(new ReadmeCandidateFile(file.RelativePath, CountLines(text)));
            }
            catch (IOException)
            {
                continue;
            }
        }

        return (candidates, contentCache);
    }

    private static int CountLines(string text) => text.GetLineCount();

    private static string BuildProjectInfo(Dictionary<string, string> contentCache, IReadOnlyList<string> selectedPaths)
    {
        var parts = selectedPaths
            .Where(contentCache.ContainsKey)
            .Select(path =>
                $"""
                PATH:
                {path}

                CONTENT:
                {contentCache[path]}
                """);

        return string.Join("\n\n", parts);
    }
}

internal static class StringLineExtensions
{
    public static int GetLineCount(this string text)
    {
        var lines = 0;
        var index = -1;

        while ((index = text.IndexOf('\n', index + 1)) >= 0)
        {
            lines++;
        }

        // A trailing newline does not start another line, matching Python's splitlines().
        return text.Length > 0 && !text.EndsWith('\n') ? lines + 1 : lines;
    }
}
