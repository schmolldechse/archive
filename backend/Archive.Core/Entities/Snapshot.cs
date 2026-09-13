using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Entities;

[Table("snapshots", Schema = "archive")]
[Index(nameof(OriginUrl), nameof(CreatedAt))]
[Index(nameof(CreatedAt))]
public sealed class Snapshot
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("project_id")]
    public Guid ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    [InverseProperty(nameof(Entities.Project.Snapshots))]
    public Project Project { get; set; } = null!;

    [Column("source_type")]
    public SourceType SourceType { get; set; }

    [MaxLength(4_096)]
    [Column("origin_url")]
    public string? OriginUrl { get; set; }

    [Required]
    [MaxLength(512)]
    [Column("title")]
    public required string Title { get; set; }

    [MaxLength(2048)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("quality")]
    public SnapshotQuality Quality { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("capture_started_at")]
    public DateTimeOffset? CaptureStartedAt { get; set; }

    [Column("capture_completed_at")]
    public DateTimeOffset? CaptureCompletedAt { get; set; }

    [Column("duration_ms")]
    public long DurationMilliseconds { get; set; }

    [Column("storage_bytes")]
    public long StorageBytes { get; set; }

    [Column("resource_count")]
    public int ResourceCount { get; set; }

    [Required]
    [MaxLength(512)]
    [Column("content_prefix")]
    public required string ContentPrefix { get; set; }

    [InverseProperty(nameof(SnapshotTag.Snapshot))]
    public List<SnapshotTag> SnapshotTags { get; set; } = [];
}
