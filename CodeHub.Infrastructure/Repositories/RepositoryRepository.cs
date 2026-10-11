namespace CodeHub.Infrastructure.Repositories;

using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;
using CodeHub.Domain.Entities;
using CodeHub.Domain.Enums;
using CodeHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class RepositoryRepository(CodeHubDbContext writeContext, CodeHubReadDbContext readContext)
    : IRepositoryRepository
{
    public Task<Repository?> GetByIdAsync(int id, int? ownerId = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Repository> query = readContext.Repositories.AsNoTracking().Include(r => r.Owner);

        if (ownerId is not null)
        {
            query = query.Where(r => r.OwnerId == ownerId);
        }

        return query.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }


    public Task<Repository?> GetByNameAsync(
        string name,
        int? ownerId = null,
        string? ownerName = null,
        CancellationToken cancellationToken = default)
    {
        // Visibility is intentionally not filtered here: the legacy API resolved
        // repositories by owner/name regardless of visibility.
        IQueryable<Repository> query = readContext.Repositories.AsNoTracking().Include(r => r.Owner);

        if (ownerId is not null)
        {
            query = query.Where(r => r.OwnerId == ownerId);
        }

        if (ownerName is not null)
        {
            query = query.Where(r => r.Owner.Username == ownerName);
        }

        return query.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }

    public Task<bool> ExistsAsync(int id, int? ownerId = null, CancellationToken cancellationToken = default)
    {
        var query = readContext.Repositories.AsNoTracking();

        if (ownerId is not null)
        {
            query = query.Where(r => r.OwnerId == ownerId);
        }

        return query.AnyAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Repository> AddAsync(Repository repository, CancellationToken cancellationToken = default)
    {
        writeContext.Repositories.Add(repository);
        await writeContext.SaveChangesAsync(cancellationToken);

        // Reload with the owner so the response mapping has owner_name available.
        return await readContext.Repositories
            .AsNoTracking()
            .Include(r => r.Owner)
            .FirstAsync(r => r.Id == repository.Id, cancellationToken);
    }

    public async Task UpdateAsync(Repository repository, CancellationToken cancellationToken = default)
    {
        // Commands may receive entities loaded on the read side; the write side
        // reloads and patches so both sides can live in separate databases.
        var tracked = await writeContext.Repositories.FirstAsync(r => r.Id == repository.Id, cancellationToken);

        tracked.Update(
            repository.Name,
            repository.Description,
            repository.Visibility,
            repository.Language);

        await writeContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Repository repository, CancellationToken cancellationToken = default)
    {
        var tracked = await writeContext.Repositories
            .Include(r => r.Files)
            .Include(r => r.Stars)
            .FirstAsync(r => r.Id == repository.Id, cancellationToken);

        writeContext.Repositories.Remove(tracked);
        await writeContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<Repository>> GetPagedAsync(
        int? ownerId,
        int page,
        int take,
        string? search,
        CancellationToken cancellationToken = default)
    {
        // Listing semantics of the legacy API: owner-scoped when ownerId is given,
        // otherwise only public repositories.
        IQueryable<Repository> query = readContext.Repositories.AsNoTracking().Include(r => r.Owner);
        query = ownerId is null
            ? query.Where(r => r.Visibility == RepositoryVisibility.Public)
            : query.Where(r => r.OwnerId == ownerId);

        if (!string.IsNullOrEmpty(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(r =>
                EF.Functions.ILike(r.Name, pattern) ||
                (r.Description != null && EF.Functions.ILike(r.Description, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.UpdatedAt)
            .Skip((page - 1) * take)
            .Take(take)
            .ToListAsync(cancellationToken);

        return new PagedResult<Repository>(items, totalCount);
    }

    public Task<IReadOnlyList<Repository>> GetAllPublicAsync(CancellationToken cancellationToken = default) =>
        ListPublicAsync(cancellationToken);

    private async Task<IReadOnlyList<Repository>> ListPublicAsync(CancellationToken cancellationToken)
    {
        var repositories = await readContext.Repositories
            .AsNoTracking()
            .Include(r => r.Owner)
            .Where(r => r.Visibility == RepositoryVisibility.Public)
            .ToListAsync(cancellationToken);

        return repositories;
    }

}
