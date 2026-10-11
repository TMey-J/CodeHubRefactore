namespace CodeHub.Infrastructure.Repositories;

using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;
using CodeHub.Domain.Entities;
using CodeHub.Domain.Enums;
using CodeHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class DashboardRepository(CodeHubReadDbContext readContext) : IDashboardRepository
{
    public async Task<DashboardStatistics> GetPublicStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var repositories = await readContext.Repositories.CountAsync(cancellationToken);
        var files = await readContext.Files.CountAsync(cancellationToken);
        var users = await readContext.Users.CountAsync(cancellationToken);
        var stars = await readContext.Repositories.SumAsync(r => (int?)r.StarsCount, cancellationToken) ?? 0;

        var vulnerabilityRequests = await CountRequestsAsync(null, AiServiceNames.Vulnerability, cancellationToken);
        var optimizationRequests = await CountRequestsAsync(null, AiServiceNames.Optimization, cancellationToken);
        var readmeRequests = await CountRequestsAsync(null, AiServiceNames.Readme, cancellationToken);
        var repositorySearchRequests = await CountRequestsAsync(null, AiServiceNames.RepositorySearch, cancellationToken);

        return new DashboardStatistics(
            repositories,
            files,
            users,
            stars,
            vulnerabilityRequests,
            optimizationRequests,
            readmeRequests,
            repositorySearchRequests);
    }

    public async Task<DashboardUserStatistics> GetUserStatisticsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var repositories = await readContext.Repositories
            .CountAsync(r => r.OwnerId == userId, cancellationToken);

        var files = await readContext.Files
            .CountAsync(f => f.Repository!.OwnerId == userId, cancellationToken);

        var stars = await readContext.Repositories
            .Where(r => r.OwnerId == userId)
            .SumAsync(r => (int?)r.StarsCount, cancellationToken) ?? 0;

        var vulnerabilityRequests = await CountRequestsAsync(userId, AiServiceNames.Vulnerability, cancellationToken);
        var optimizationRequests = await CountRequestsAsync(userId, AiServiceNames.Optimization, cancellationToken);
        var readmeRequests = await CountRequestsAsync(userId, AiServiceNames.Readme, cancellationToken);
        var repositorySearchRequests = await CountRequestsAsync(userId, AiServiceNames.RepositorySearch, cancellationToken);

        return new DashboardUserStatistics(
            repositories,
            files,
            stars,
            vulnerabilityRequests,
            optimizationRequests,
            readmeRequests,
            repositorySearchRequests);
    }

    private Task<int> CountRequestsAsync(int? userId, string service, CancellationToken cancellationToken) =>
        userId is null
            ? readContext.AiRequests.CountAsync(a => a.Service == service, cancellationToken)
            : readContext.AiRequests.CountAsync(a => a.Service == service && a.UserId == userId, cancellationToken);
}
