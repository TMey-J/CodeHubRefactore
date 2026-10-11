namespace CodeHub.Api.Endpoints;

using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;
using CodeHub.Application.Common.Mapping;
using CodeHub.Application.Contracts;
using CodeHub.Application.Features.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/dashboard").WithTags("Dashboard");

        group.MapGet("",
            async (IDispatcher dispatcher, CancellationToken cancellationToken) =>
            {
                var statistics = await dispatcher.QueryAsync<GetDashboardStatisticsQuery, DashboardStatisticsResponse>(
                    new GetDashboardStatisticsQuery(),
                    cancellationToken);

                return TypedResults.Ok(ApiResponse<DashboardStatisticsResponse>.Ok(statistics));
            });

        group.MapGet("/user",
            async (IDispatcher dispatcher, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            {
                var user = await currentUser.GetUserAsync(cancellationToken);

                var statistics = await dispatcher.QueryAsync<GetUserDashboardStatisticsQuery, DashboardUserStatisticsResponse>(
                    new GetUserDashboardStatisticsQuery(user.Id),
                    cancellationToken);

                return TypedResults.Ok(ApiResponse<DashboardUserStatisticsResponse>.Ok(statistics));
            })
            .RequireAuthorization();

        return app;
    }
}
