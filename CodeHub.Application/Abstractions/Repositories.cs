namespace CodeHub.Application.Abstractions;

using CodeHub.Application.Common;
using CodeHub.Domain.Entities;

public sealed record UserProfileData(
    User User,
    int PublicRepositoriesCount,
    int PublicFilesCount,
    int ReceivedStars,
    IReadOnlyList<Repository> PublicRepositories);

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);

    Task<UserProfileData?> GetProfileAsync(string username, int page, int take, CancellationToken cancellationToken = default);
}

public interface IRepositoryRepository
{
    Task<Repository?> GetByIdAsync(int id, int? ownerId = null, CancellationToken cancellationToken = default);

    Task<Repository?> GetByNameAsync(
        string name,
        int? ownerId = null,
        string? ownerName = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, int? ownerId = null, CancellationToken cancellationToken = default);

    Task<Repository> AddAsync(Repository repository, CancellationToken cancellationToken = default);

    Task UpdateAsync(Repository repository, CancellationToken cancellationToken = default);

    Task RemoveAsync(Repository repository, CancellationToken cancellationToken = default);

    /// <summary>
    /// Paged listing; when ownerId is null only public repositories are returned.
    /// </summary>
    Task<PagedResult<Repository>> GetPagedAsync(
        int? ownerId,
        int page,
        int take,
        string? search,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Repository>> GetAllPublicAsync(CancellationToken cancellationToken = default);
}

public interface IFileRepository
{
    Task<RepositoryFile?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<RepositoryFile?> GetByPathAsync(int repositoryId, string relativePath, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RepositoryFile>> GetAllAsync(int repositoryId, CancellationToken cancellationToken = default);

    Task AddAsync(RepositoryFile file, CancellationToken cancellationToken = default);

    Task UpdateAsync(RepositoryFile file, CancellationToken cancellationToken = default);

    Task RemoveAsync(RepositoryFile file, CancellationToken cancellationToken = default);

    Task RemoveAllForRepositoryAsync(int repositoryId, CancellationToken cancellationToken = default);
}

public interface IStarRepository
{
    Task<bool> ExistsAsync(int repositoryId, int userId, CancellationToken cancellationToken = default);

    Task AddAsync(int repositoryId, int userId, CancellationToken cancellationToken = default);

    Task RemoveAsync(int repositoryId, int userId, CancellationToken cancellationToken = default);
}

public interface IAiRequestRepository
{
    Task AddAsync(int? userId, string service, CancellationToken cancellationToken = default);
}

public interface IDashboardRepository
{
    Task<DashboardStatistics> GetPublicStatisticsAsync(CancellationToken cancellationToken = default);

    Task<DashboardUserStatistics> GetUserStatisticsAsync(int userId, CancellationToken cancellationToken = default);
}
