using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Archive.Core.Entities.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Entities;

[Table("snapshot_history", Schema = "archive")]
[Index(nameof(SnapshotId), nameof(Sequence), IsUnique = true)]
public sealed class SnapshotHistoryEntry
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("snapshot_id")]
    public Guid SnapshotId { get; set; }

    [ForeignKey(nameof(SnapshotId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public Snapshot Snapshot { get; set; } = null!;

    [Column("sequence")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long Sequence { get; set; }

    [Column("actor_user_id")]
    public Guid? ActorUserId { get; set; }

    [ForeignKey(nameof(ActorUserId))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public ArchiveUser? Actor { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Required]
    [Column("content", TypeName = "jsonb")]
    public required JsonElement Content { get; set; }
}
