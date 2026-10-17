using Archive.Core.Entities;
using Archive.Core.Entities.Authentication;
using Archive.Core.Entities.Authentication.Permissible;
using Archive.Core.Entities.Processing;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core;

public sealed class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<SnapshotProcessing> SnapshotProcessing => Set<SnapshotProcessing>();

    public DbSet<Snapshot> Snapshots => Set<Snapshot>();
    public DbSet<SnapshotTag> SnapshotTags => Set<SnapshotTag>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<ArchiveUser> Users => Set<ArchiveUser>();
    public DbSet<AuthProvider> AuthProviders => Set<AuthProvider>();
    public DbSet<UserAuthIdentity> UserAuthIdentities => Set<UserAuthIdentity>();

    public DbSet<ArchiveUserPermissionOverride> UserPermissionOverrides => Set<ArchiveUserPermissionOverride>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
