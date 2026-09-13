using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Entities;

[Table("archive_jobs", Schema = "processing")]
[Index(nameof(Status), nameof(CreatedAt))]
public sealed class ArchiveJob
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("source_type")]
    public SourceType SourceType { get; set; }

    [MaxLength(4096)]
    [Column("origin_url")]
    public string? OriginUrl { get; set; }

    [MaxLength(512)]
    [Column("uploaded_object_key")]
    public string? UploadedObjectKey { get; set; }

    [Required]
    [MaxLength(512)]
    [Column("title")]
    public required string Title { get; set; }

    [Column("description")]
    [MaxLength(2048)]
    public string? Description { get; set; }

    [Column("status")]
    public JobStatus Status { get; set; } = JobStatus.Queued;

    [Column("progress_step")]
    public ProgressStep ProgressStep { get; set; } = ProgressStep.SourceCheck;

    [Column("progress_percent")]
    public int ProgressPercent { get; set; }

    [MaxLength(2048)]
    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("started_at")]
    public DateTimeOffset? StartedAt { get; set; }

    [Column("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }

    [InverseProperty(nameof(ArchiveJobTag.Job))]
    public List<ArchiveJobTag> Tags { get; set; } = [];
}
