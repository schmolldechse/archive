using Archive.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Archive.Core.Configurations;

public sealed class SnapshotHistoryConfiguration : IEntityTypeConfiguration<SnapshotHistoryEntry>
{
    public void Configure(EntityTypeBuilder<SnapshotHistoryEntry> builder) => builder.ToTable(table =>
    {
        table.HasCheckConstraint("CK_snapshot_history_sequence", "sequence > 0");
    });
}
