namespace CodeHub.Application.Contracts;

using CodeHub.Application.Common;

public sealed record DashboardStatisticsResponse(
    int Repositories,
    int Files,
    int Users,
    int Stars,
    int VulnerabilityRequests,
    int OptimizationRequests,
    int ReadmeRequests,
    int RepositorySearchRequests);

public sealed record DashboardUserStatisticsResponse(
    int Repositories,
    int Files,
    int Stars,
    int VulnerabilityRequests,
    int OptimizationRequests,
    int ReadmeRequests,
    int RepositorySearchRequests);

public sealed record UserProfileResponse(
    string Username,
    string CreatedAt,
    int RepositoriesCount,
    int FilesCount,
    int ReceivedStars,
    PagedResponse<RepositoryResponse> Repositories);
