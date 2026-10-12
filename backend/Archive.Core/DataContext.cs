using Archive.Core.Entities;
using Archive.Core.Entities.Authentication;
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

    public DbSet<ArchiveUser> Users => Set<ArchiveUser>();
    public DbSet<AuthProvider> AuthProviders => Set<AuthProvider>();
    public DbSet<UserAuthIdentity> UserAuthIdentities => Set<UserAuthIdentity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
