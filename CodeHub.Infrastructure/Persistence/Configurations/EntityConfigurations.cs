namespace CodeHub.Infrastructure.Persistence.Configurations;

using CodeHub.Domain.Entities;
using CodeHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(u => u.Username).HasColumnName("username").IsRequired();
        builder.Property(u => u.Email).HasColumnName("email").IsRequired();
        builder.Property(u => u.HashedPassword).HasColumnName("hashed_password").IsRequired();

        // Legacy schema stores user timestamps as timestamp WITHOUT time zone.
        builder.Property(u => u.CreatedAt).HasColumnName("created_at")
            .HasColumnType("timestamp without time zone").IsRequired();
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at")
            .HasColumnType("timestamp without time zone").IsRequired();

        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}

internal sealed class RepositoryConfiguration : IEntityTypeConfiguration<Repository>
{
    public void Configure(EntityTypeBuilder<Repository> builder)
    {
        builder.ToTable("repositories");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(r => r.Description).HasColumnName("description");
        builder.Property(r => r.OwnerId).HasColumnName("owner_id").IsRequired();
        builder.Property(r => r.Language).HasColumnName("language").HasMaxLength(255).IsRequired();

        builder.Property(r => r.Visibility).HasColumnName("visibility")
            .HasConversion(
                visibility => visibility.ToString().ToUpperInvariant(),
                value => Enum.Parse<RepositoryVisibility>(value, ignoreCase: true))
            .IsRequired();

        builder.Property(r => r.SearchText).HasColumnName("search_text").HasDefaultValue(string.Empty);
        builder.Property(r => r.StarsCount).HasColumnName("stars_count").HasDefaultValue(0);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne(r => r.Owner)
            .WithMany()
            .HasForeignKey(r => r.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Files)
            .WithOne(f => f.Repository)
            .HasForeignKey(f => f.RepositoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Stars)
            .WithOne(s => s.Repository)
            .HasForeignKey(s => s.RepositoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(r => r.Owner).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Repository.Files))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Repository.Stars))?.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(r => r.Name);
        builder.HasIndex(r => r.OwnerId);
        builder.HasIndex(r => r.Visibility);
    }
}

internal sealed class RepositoryFileConfiguration : IEntityTypeConfiguration<RepositoryFile>
{
    public void Configure(EntityTypeBuilder<RepositoryFile> builder)
    {
        builder.ToTable("files");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(f => f.RepositoryId).HasColumnName("repository_id").IsRequired();
        builder.Property(f => f.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
        builder.Property(f => f.StoredName).HasColumnName("stored_name").HasMaxLength(255).IsRequired();
        builder.Property(f => f.FilePath).HasColumnName("file_path").IsRequired();
        builder.Property(f => f.FileSize).HasColumnName("file_size").IsRequired();
        builder.Property(f => f.RelativePath).HasColumnName("relative_path").IsRequired();
        builder.Property(f => f.UploadedAt).HasColumnName("uploaded_at").IsRequired();

        builder.HasIndex(f => f.RepositoryId);
    }
}

internal sealed class RepositoryStarConfiguration : IEntityTypeConfiguration<RepositoryStar>
{
    public void Configure(EntityTypeBuilder<RepositoryStar> builder)
    {
        builder.ToTable("repository_stars");

        builder.HasKey(s => new { s.RepositoryId, s.UserId });

        builder.Property(s => s.RepositoryId).HasColumnName("repository_id");
        builder.Property(s => s.UserId).HasColumnName("user_id");
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne(s => s.Repository)
            .WithMany(r => r.Stars)
            .HasForeignKey(s => s.RepositoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AiRequestConfiguration : IEntityTypeConfiguration<AiRequest>
{
    public void Configure(EntityTypeBuilder<AiRequest> builder)
    {
        builder.ToTable("ai_requests");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(a => a.UserId).HasColumnName("user_id");
        builder.Property(a => a.Service).HasColumnName("service").HasMaxLength(30).IsRequired();
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.Service);
    }
}
