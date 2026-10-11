namespace CodeHub.Application.Common.Mapping;

using CodeHub.Application.Contracts;
using CodeHub.Domain.Entities;

public static class UserMapping
{
    public static UserResponse ToResponse(this User user) =>
        new(user.Id, user.Username, user.Email, user.CreatedAt, user.UpdatedAt);
}

public static class RepositoryMapping
{
    public static RepositoryResponse ToResponse(this Repository repository) =>
        new(
            repository.Id,
            repository.Name,
            repository.Description,
            repository.Visibility,
            repository.Language,
            repository.OwnerId,
            repository.Owner.Username,
            repository.CreatedAt.ToDisplayString(),
            repository.UpdatedAt.ToDisplayString(),
            repository.StarsCount);
}

public static class FileMapping
{
    public static FileResponse ToResponse(this RepositoryFile file) =>
        new(
            file.Id,
            file.RepositoryId,
            file.FileName,
            file.FileSize,
            file.UploadedAt.ToDisplayString(),
            file.RelativePath);
}

public static class DashboardMapping
{
    public static DashboardStatisticsResponse ToResponse(this DashboardStatistics statistics) =>
        new(
            statistics.Repositories,
            statistics.Files,
            statistics.Users,
            statistics.Stars,
            statistics.VulnerabilityRequests,
            statistics.OptimizationRequests,
            statistics.ReadmeRequests,
            statistics.RepositorySearchRequests);

    public static DashboardUserStatisticsResponse ToResponse(this DashboardUserStatistics statistics) =>
        new(
            statistics.Repositories,
            statistics.Files,
            statistics.Stars,
            statistics.VulnerabilityRequests,
            statistics.OptimizationRequests,
            statistics.ReadmeRequests,
            statistics.RepositorySearchRequests);
}
