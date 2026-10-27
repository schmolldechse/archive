using Archive.Core.Entities.Processing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Archive.Core.Configurations.Processing;

public sealed class SnapshotDeletionConfiguration : IEntityTypeConfiguration<SnapshotDeletion>
{
    public void Configure(EntityTypeBuilder<SnapshotDeletion> builder)
    {
        builder.HasIndex(item => new { item.NextAttemptAt, item.SnapshotId });
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_snapshot_deletions_attempt_count", "attempt_count >= 0"));
    }
}
