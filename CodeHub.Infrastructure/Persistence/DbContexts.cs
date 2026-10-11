namespace CodeHub.Infrastructure.Persistence;

using CodeHub.Application.Abstractions;
using CodeHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Write-side DbContext. Commands persist through this context (IUnitOfWork).
/// Shares the same mapping model as the read context so the two stores can be
/// split into separate databases without touching entity configurations.
/// </summary>
public class CodeHubDbContext(DbContextOptions<CodeHubDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Repository> Repositories => Set<Repository>();
    public DbSet<RepositoryFile> Files => Set<RepositoryFile>();
    public DbSet<RepositoryStar> Stars => Set<RepositoryStar>();
    public DbSet<AiRequest> AiRequests => Set<AiRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CodeHubDbContext).Assembly);
    }
}

/// <summary>
/// Read-side DbContext used by query handlers.
/// Points at its own connection string; today it is the same database as the write side,
/// but a read replica only requires changing <c>Database:ReadConnectionString</c>.
/// </summary>
public class CodeHubReadDbContext(DbContextOptions<CodeHubReadDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Repository> Repositories => Set<Repository>();
    public DbSet<RepositoryFile> Files => Set<RepositoryFile>();
    public DbSet<RepositoryStar> Stars => Set<RepositoryStar>();
    public DbSet<AiRequest> AiRequests => Set<AiRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CodeHubDbContext).Assembly);
    }
}
