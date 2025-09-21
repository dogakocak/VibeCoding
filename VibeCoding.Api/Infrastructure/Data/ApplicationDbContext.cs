using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VibeCoding.Api.Domain.Entities;

namespace VibeCoding.Api.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<TrainingScenario> TrainingScenarios => Set<TrainingScenario>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<ScenarioAttempt> ScenarioAttempts => Set<ScenarioAttempt>();
    public DbSet<ScenarioTag> ScenarioTags => Set<ScenarioTag>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<ImportBatchLog> ImportBatchLogs => Set<ImportBatchLog>();
    public DbSet<TelemetryEvent> TelemetryEvents => Set<TelemetryEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<TrainingScenario>(entity =>
        {
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.Title).IsRequired().HasMaxLength(160);
            entity.Property(x => x.Slug).IsRequired().HasMaxLength(160);
            entity.Property(x => x.Description).HasMaxLength(2048);
            entity.Property(x => x.Notes).HasMaxLength(1024);
            entity.Property(x => x.ExternalReference).HasMaxLength(160);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasOne(x => x.MediaAsset)
                .WithMany(x => x.Scenarios)
                .HasForeignKey(x => x.MediaAssetId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedBy)
                .WithMany()
                .HasForeignKey(x => x.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.LastModifiedBy)
                .WithMany()
                .HasForeignKey(x => x.LastModifiedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ScenarioTag>(entity =>
        {
            entity.HasKey(x => new { x.ScenarioId, x.Tag });
            entity.Property(x => x.Tag).HasMaxLength(64);
        });

        builder.Entity<MediaAsset>(entity =>
        {
            entity.HasIndex(x => x.BlobName).IsUnique();
            entity.Property(x => x.BlobName).IsRequired().HasMaxLength(256);
            entity.Property(x => x.ThumbnailBlobName).HasMaxLength(256);
            entity.Property(x => x.ContentType).HasMaxLength(128);
            entity.Property(x => x.Sha256Hash).HasMaxLength(128);
        });

        builder.Entity<ScenarioAttempt>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.ScenarioId });
            entity.Property(x => x.Score).HasPrecision(5, 2);
            entity.Property(x => x.SessionId).HasMaxLength(64);
            entity.Property(x => x.Explanation).HasMaxLength(1024);
            entity.Property(x => x.IpHash).HasMaxLength(128);
            entity.Property(x => x.UserAgentHash).HasMaxLength(128);
        });

        builder.Entity<ImportBatch>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.SourceBlobName).HasMaxLength(256).IsRequired();
            entity.Property(x => x.FailureReason).HasMaxLength(1024);
            entity.HasMany(x => x.Logs)
                .WithOne(x => x.ImportBatch)
                .HasForeignKey(x => x.ImportBatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ImportBatchLog>(entity =>
        {
            entity.Property(x => x.Level).HasMaxLength(32);
            entity.Property(x => x.Message).HasMaxLength(1024);
        });

        builder.Entity<TelemetryEvent>(entity =>
        {
            entity.HasIndex(x => x.OccurredAt);
            entity.Property(x => x.UserHash).HasMaxLength(128);
            entity.Property(x => x.SessionId).HasMaxLength(64);
            entity.Property(x => x.Payload).HasMaxLength(2048);
            entity.Property(x => x.CorrelationId).HasMaxLength(64);
        });
    }
}
