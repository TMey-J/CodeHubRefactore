namespace CodeHub.Infrastructure;

using CodeHub.Application.Abstractions;
using CodeHub.Infrastructure.Persistence;
using CodeHub.Infrastructure.Repositories;
using CodeHub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var writeConnectionString = configuration.GetConnectionString("Write")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'Write' is not configured.");

        // Read and write point at the same database today; a read replica only
        // needs its own connection string (see appsettings.json).
        var readConnectionString = configuration.GetConnectionString("Read") ?? writeConnectionString;

        services.AddDbContext<CodeHubDbContext>(options => options.UseNpgsql(writeConnectionString));
        services.AddDbContext<CodeHubReadDbContext>(options => options.UseNpgsql(readConnectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CodeHubDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRepositoryRepository, RepositoryRepository>();
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<IStarRepository, StarRepository>();
        services.AddScoped<IAiRequestRepository, AiRequestRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<ICurrentUser, CurrentUserService>();

        services.AddHttpContextAccessor();

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<SambaNovaOptions>()
            .Bind(configuration.GetSection(SambaNovaOptions.SectionName));

        services.AddHttpClient<IAiClient, SambaNovaAiClient>();

        return services;
    }
}
