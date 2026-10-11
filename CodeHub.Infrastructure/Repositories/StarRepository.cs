namespace CodeHub.Infrastructure.Repositories;

using CodeHub.Application.Abstractions;
using CodeHub.Domain.Entities;
using CodeHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class StarRepository(CodeHubDbContext writeContext, CodeHubReadDbContext readContext)
    : IStarRepository
{
    public Task<bool> ExistsAsync(int repositoryId, int userId, CancellationToken cancellationToken = default) =>
        readContext.Stars.AsNoTracking()
            .AnyAsync(s => s.RepositoryId == repositoryId && s.UserId == userId, cancellationToken);

    public async Task AddAsync(int repositoryId, int userId, CancellationToken cancellationToken = default)
    {
        writeContext.Stars.Add(RepositoryStar.Create(repositoryId, userId));

        await writeContext.Repositories
            .Where(r => r.Id == repositoryId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.StarsCount, r => r.StarsCount + 1), cancellationToken);

        await writeContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(int repositoryId, int userId, CancellationToken cancellationToken = default)
    {
        await writeContext.Stars
            .Where(s => s.RepositoryId == repositoryId && s.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        await writeContext.Repositories
            .Where(r => r.Id == repositoryId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.StarsCount, r => r.StarsCount - 1), cancellationToken);
    }
}
