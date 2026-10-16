using Archive.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Archive.Core.Configurations;

public sealed class SnapshotConfiguration : IEntityTypeConfiguration<Snapshot>
{
    public void Configure(EntityTypeBuilder<Snapshot> builder) =>
        builder.HasIndex(snapshot => snapshot.OriginUrl).HasMethod("hash");
}
