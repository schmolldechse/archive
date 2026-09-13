using Archive.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core;

public sealed class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<ArchiveJob> ArchiveJobs => Set<ArchiveJob>();
    public DbSet<ArchiveJobTag> ArchiveJobTags => Set<ArchiveJobTag>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Snapshot> Snapshots => Set<Snapshot>();
    public DbSet<SnapshotTag> SnapshotTags => Set<SnapshotTag>();

    public DbSet<Tag> Tags => Set<Tag>();
}
