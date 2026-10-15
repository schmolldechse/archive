using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Entities.Processing;

[Table("snapshot_processing", Schema = "processing")]
public sealed class SnapshotProcessing
{
    [Key]
    [Column("snapshot_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid SnapshotId { get; set; }

    [ForeignKey(nameof(SnapshotId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public Snapshot Snapshot { get; set; } = null!;

    [Column("queue_sequence")] 
    public required long QueueSequence { get; set; }

    [Column("status")] 
    public required ProcessingStatus Status { get; set; } = ProcessingStatus.Queued;

    [Column("progress_step")] 
    public required ProgressStep ProgressStep { get; set; } = ProgressStep.SourceCheck;
    
    [Column("owner_token")] 
    public Guid? OwnerToken { get; set; }

    [Column("cancel_requested")] 
    public bool CancelRequested { get; set; }
    
    [Column("started_at")] 
    public DateTimeOffset? StartedAt { get; set; }
    
    [Column("completed_at")] 
    public DateTimeOffset? CompletedAt { get; set; }

    [Column("expires_at")] 
    public DateTimeOffset? ExpiresAt { get; set; }

    [MaxLength(512)]
    [Column("uploaded_object_key")] 
    public string? UploadedObjectKey { get; set; }
}
