using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Archive.Core.Entities.Processing;

/// <summary>
/// Minimal durable work item retained only until every object belonging to a deleted snapshot is removed.
/// </summary>
[Table("snapshot_deletions", Schema = "processing")]
public sealed class SnapshotDeletion
{
    [Key]
    [Column("snapshot_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid SnapshotId { get; set; }

    [Required]
    [MaxLength(512)]
    [Column("content_prefix")]
    public required string ContentPrefix { get; set; }

    [MaxLength(512)]
    [Column("uploaded_object_key")]
    public string? UploadedObjectKey { get; set; }

    [Column("cleanup_after")]
    public DateTimeOffset CleanupAfter { get; set; }

    [Column("next_attempt_at")]
    public DateTimeOffset NextAttemptAt { get; set; }

    [Column("attempt_count")]
    public int AttemptCount { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}
