using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Entities;

[Table("snapshot_tags", Schema = "archive")]
[PrimaryKey(nameof(SnapshotId), nameof(TagId))]
public sealed class SnapshotTag
{
    [Column("snapshot_id")]
    public Guid SnapshotId { get; set; }

    [ForeignKey(nameof(SnapshotId))]
    [InverseProperty(nameof(Entities.Snapshot.SnapshotTags))]
    public Snapshot Snapshot { get; set; } = null!;

    [Column("tag_id")]
    public Guid TagId { get; set; }

    [ForeignKey(nameof(TagId))]
    [InverseProperty(nameof(Entities.Tag.SnapshotTags))]
    public Tag Tag { get; set; } = null!;
}
