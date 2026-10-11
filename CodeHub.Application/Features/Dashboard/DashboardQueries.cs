namespace CodeHub.Application.Features.Dashboard;

using CodeHub.Application.Abstractions;
using CodeHub.Application.Common.Mapping;
using CodeHub.Application.Contracts;
using CodeHub.Application.Common;
using CodeHub.Domain.Entities;

public sealed record GetDashboardStatisticsQuery : IQuery<DashboardStatisticsResponse>;

public sealed class GetDashboardStatisticsHandler(IDashboardRepository dashboard)
    : IQueryHandler<GetDashboardStatisticsQuery, DashboardStatisticsResponse>
{
    public async Task<DashboardStatisticsResponse> HandleAsync(GetDashboardStatisticsQuery query, CancellationToken cancellationToken = default) =>
        (await dashboard.GetPublicStatisticsAsync(cancellationToken)).ToResponse();
}

public sealed record GetUserDashboardStatisticsQuery(int UserId) : IQuery<DashboardUserStatisticsResponse>;

public sealed class GetUserDashboardStatisticsHandler(IDashboardRepository dashboard)
    : IQueryHandler<GetUserDashboardStatisticsQuery, DashboardUserStatisticsResponse>
{
    public async Task<DashboardUserStatisticsResponse> HandleAsync(GetUserDashboardStatisticsQuery query, CancellationToken cancellationToken = default) =>
        (await dashboard.GetUserStatisticsAsync(query.UserId, cancellationToken)).ToResponse();
}
