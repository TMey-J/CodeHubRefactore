namespace CodeHub.Infrastructure.Repositories;

using CodeHub.Application.Abstractions;
using CodeHub.Domain.Entities;
using CodeHub.Domain.Enums;
using CodeHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class UserRepository(CodeHubDbContext writeContext, CodeHubReadDbContext readContext)
    : IUserRepository
{
    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        readContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        readContext.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        readContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        writeContext.Users.Add(user);
        await writeContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<UserProfileData?> GetProfileAsync(string username, int page, int take, CancellationToken cancellationToken = default)
    {
        var user = await readContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var publicRepositories = readContext.Repositories
            .AsNoTracking()
            .Include(r => r.Owner)
            .Where(r => r.OwnerId == user.Id && r.Visibility == RepositoryVisibility.Public);

        var repositoriesCount = await publicRepositories.CountAsync(cancellationToken);

        var filesCount = await readContext.Files
            .AsNoTracking()
            .CountAsync(f => f.Repository!.OwnerId == user.Id
                             && f.Repository.Visibility == RepositoryVisibility.Public,
                cancellationToken);

        var receivedStars = await readContext.Repositories
            .AsNoTracking()
            .Where(r => r.OwnerId == user.Id && r.Visibility == RepositoryVisibility.Public)
            .SumAsync(r => (int?)r.StarsCount, cancellationToken) ?? 0;

        var repositories = await publicRepositories
            .OrderByDescending(r => r.UpdatedAt)
            .Skip((page - 1) * take)
            .Take(take)
            .ToListAsync(cancellationToken);

        return new UserProfileData(user, repositoriesCount, filesCount, receivedStars, repositories);
    }
}
