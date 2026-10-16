using System.ComponentModel.DataAnnotations;
using Archive.Core.Models.Snapshots;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Archive.Core.Entities.Authentication;

namespace Archive.Core.Entities;

[Table("snapshots", Schema = "archive")]
[Index(nameof(CreatedAt))]
public sealed class Snapshot
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("created_by_user_id")]
    public Guid? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public ArchiveUser? CreatedBy { get; set; }

    [Column("project_id")]
    public Guid ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    [InverseProperty(nameof(Entities.Project.Snapshots))]
    public Project Project { get; set; } = null!;

    [Column("source_type")]
    public SourceType SourceType { get; set; }

    [MaxLength(SnapshotValidation.MaxSourceUrlLength)]
    [Column("origin_url")]
    public string? OriginUrl { get; set; }

    [Required]
    [MaxLength(SnapshotValidation.MaxTitleLength)]
    [Column("title")]
    public required string Title { get; set; }

    [MaxLength(SnapshotValidation.MaxDescriptionLength)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("quality")]
    public SnapshotQuality? Quality { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("capture_started_at")]
    public DateTimeOffset? CaptureStartedAt { get; set; }

    [Column("capture_completed_at")]
    public DateTimeOffset? CaptureCompletedAt { get; set; }

    [Column("duration_ms")]
    public long? DurationMilliseconds { get; set; }

    [Column("storage_bytes")]
    public long? StorageBytes { get; set; }

    [Column("resource_count")]
    public int? ResourceCount { get; set; }

    [MaxLength(512)]
    [Column("content_prefix")]
    public string? ContentPrefix { get; set; }

    [InverseProperty(nameof(SnapshotTag.Snapshot))]
    public List<SnapshotTag> SnapshotTags { get; set; } = [];
}
