namespace CodeHub.Infrastructure.Repositories;

using CodeHub.Application.Abstractions;
using CodeHub.Domain.Entities;
using CodeHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class FileRepository(CodeHubDbContext writeContext, CodeHubReadDbContext readContext)
    : IFileRepository
{
    public Task<RepositoryFile?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        readContext.Files.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public Task<RepositoryFile?> GetByPathAsync(int repositoryId, string relativePath, CancellationToken cancellationToken = default) =>
        readContext.Files.AsNoTracking()
            .FirstOrDefaultAsync(f => f.RepositoryId == repositoryId && f.RelativePath == relativePath, cancellationToken);

    public async Task<IReadOnlyList<RepositoryFile>> GetAllAsync(int repositoryId, CancellationToken cancellationToken = default)
    {
        var files = await readContext.Files.AsNoTracking()
            .Where(f => f.RepositoryId == repositoryId)
            .ToListAsync(cancellationToken);

        return files;
    }

    public async Task AddAsync(RepositoryFile file, CancellationToken cancellationToken = default)
    {
        writeContext.Files.Add(file);
        await writeContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(RepositoryFile file, CancellationToken cancellationToken = default)
    {
        var tracked = await writeContext.Files.FirstAsync(f => f.Id == file.Id, cancellationToken);

        tracked.ReplaceContent(file.StoredName, file.FilePath, file.FileSize);

        await writeContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(RepositoryFile file, CancellationToken cancellationToken = default)
    {
        var tracked = await writeContext.Files.FirstAsync(f => f.Id == file.Id, cancellationToken);
        writeContext.Files.Remove(tracked);
        await writeContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAllForRepositoryAsync(int repositoryId, CancellationToken cancellationToken = default)
    {
        await writeContext.Files
            .Where(f => f.RepositoryId == repositoryId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
