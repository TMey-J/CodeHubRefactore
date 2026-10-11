namespace CodeHub.Infrastructure.Persistence;

using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class MigrationExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations at startup when "Database:MigrateOnStartup" is enabled
    /// (used by the Docker image so a fresh database bootstraps itself).
    /// </summary>
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CodeHubDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CodeHubDbContext>>();

        try
        {
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred while applying database migrations");
            throw;
        }
    }
}
