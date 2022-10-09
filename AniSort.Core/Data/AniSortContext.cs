﻿using System.Threading;
using System.Threading.Tasks;
using AniDbSharp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;

namespace AniSort.Core.Data;

public class AniSortContext : DbContext
{
    private readonly ILogger<AniSortContext> logger;
    
    /// <inheritdoc />
    protected AniSortContext(ILogger<AniSortContext> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public AniSortContext(DbContextOptions options, ILogger<AniSortContext> logger) : base(options)
    {
        this.logger = logger;
    }

    public static SemaphoreSlim DatabaseLock { get; } = new(1, 1);

    public DbSet<Anime> Anime { get; set; }

    public DbSet<Synonym> Synonyms { get; set; }

    public DbSet<Category> Categories { get; set; }

    public DbSet<AnimeCategory> AnimeCategories { get; set; }

    public DbSet<Episode> Episodes { get; set; }

    public DbSet<EpisodeFile> EpisodeFiles { get; set; }

    public DbSet<AudioCodec> AudioCodecs { get; set; }

    public DbSet<ReleaseGroup> ReleaseGroups { get; set; }

    public DbSet<LocalFile> LocalFiles { get; set; }

    public DbSet<FileAction> FileActions { get; set; }

    public DbSet<Job> Jobs { get; set; }

    public DbSet<ScheduledJob> ScheduledJobs { get; set; }

    public DbSet<JobStep> JobSteps { get; set; }

    public DbSet<JobLog> JobLogs { get; set; }

    public DbSet<StepLog> JobStepLogs { get; set; }
    
    public DbSet<Setting?> Setting { get; set; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>()
            .HasIndex(e => e.Value)
            .IsUnique();

        modelBuilder.Entity<RelatedAnime>()
            .HasOne(e => e.SourceAnime)
            .WithMany(e => e.ChildrenAnime)
            .HasForeignKey(e => e.SourceAnimeId);

        modelBuilder.Entity<RelatedAnime>()
            .HasOne(e => e.DestinationAnime)
            .WithMany(e => e.ParentAnime)
            .HasForeignKey(e => e.DestinationAnimeId);

        modelBuilder.Entity<AnimeCategory>()
            .HasKey(e => new { e.AnimeId, e.CategoryId });

        modelBuilder.Entity<AnimeCategory>()
            .HasOne(e => e.Anime)
            .WithMany(e => e.Categories)
            .HasForeignKey(e => e.AnimeId);

        modelBuilder.Entity<AnimeCategory>()
            .HasOne(e => e.Category)
            .WithMany(e => e.Anime)
            .HasForeignKey(e => e.CategoryId);

        modelBuilder.Entity<EpisodeFile>()
            .Property(e => e.State)
            .HasConversion(new EnumToStringConverter<FileState>());

        modelBuilder.Entity<EpisodeFile>()
            .Ignore(e => e.Resolution);

        modelBuilder.Entity<FileAction>()
            .Property(e => e.Type)
            .HasConversion(new EnumToStringConverter<FileActionType>());

        modelBuilder.ApplyConfiguration(new JobEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new JobLogEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new StepLogEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ScheduledJobEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new SettingEntityTypeConfiguration());
    }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .EnableDetailedErrors()
            .ConfigureWarnings(c => c
                .Default(WarningBehavior.Ignore));
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        logger.LogTrace("Disposing AniSortContext");
        base.Dispose();
    }

    /// <inheritdoc />
    public override ValueTask DisposeAsync()
    {
        logger.LogTrace("Disposing AniSortContext asynchronously");
        return base.DisposeAsync();
    }
}
