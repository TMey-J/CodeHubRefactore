namespace CodeHub.Domain.Entities;

/// <summary>
/// Read model describing aggregate counts for the whole platform.
/// </summary>
public sealed record DashboardStatistics(
    int Repositories,
    int Files,
    int Users,
    int Stars,
    int VulnerabilityRequests,
    int OptimizationRequests,
    int ReadmeRequests,
    int RepositorySearchRequests);

/// <summary>
/// Read model describing aggregate counts for a single user.
/// </summary>
public sealed record DashboardUserStatistics(
    int Repositories,
    int Files,
    int Stars,
    int VulnerabilityRequests,
    int OptimizationRequests,
    int ReadmeRequests,
    int RepositorySearchRequests);
