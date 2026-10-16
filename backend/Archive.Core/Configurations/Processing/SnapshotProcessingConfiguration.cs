using Archive.Core.Entities.Processing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Archive.Core.Configurations.Processing;

public sealed class SnapshotProcessingConfiguration : IEntityTypeConfiguration<SnapshotProcessing>
{
    public void Configure(EntityTypeBuilder<SnapshotProcessing> builder)
    {
        builder.HasIndex(item => item.QueueSequence).HasFilter("status = 'QUEUED'");
        builder.HasIndex(item => new { item.ExpiresAt, item.SnapshotId }).HasFilter("expires_at IS NOT NULL");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_snapshot_processing_step", """
                (status <> 'QUEUED' OR progress_step = 'SOURCE_CHECK')
                AND ((status = 'COMPLETED' AND progress_step = 'FINISHED')
                    OR (status <> 'COMPLETED' AND progress_step <> 'FINISHED'))
                """);
            table.HasCheckConstraint("CK_snapshot_processing_owner", """
                (status = 'RUNNING' AND owner_token IS NOT NULL AND started_at IS NOT NULL)
                OR (status <> 'RUNNING' AND owner_token IS NULL)
                """);
            table.HasCheckConstraint("CK_snapshot_processing_terminal", """
                (status IN ('QUEUED', 'RUNNING') AND completed_at IS NULL AND expires_at IS NULL)
                OR (status NOT IN ('QUEUED', 'RUNNING') AND completed_at IS NOT NULL
                    AND expires_at IS NOT NULL AND expires_at >= completed_at)
                """);
            table.HasCheckConstraint("CK_snapshot_processing_cancel", "NOT cancel_requested OR status = 'RUNNING'");
        });
    }
}
