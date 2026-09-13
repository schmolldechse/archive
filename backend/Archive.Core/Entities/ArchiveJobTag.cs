using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Entities;

[Table("archive_job_tags", Schema = "processing")]
[Index(nameof(JobId), nameof(Value), IsUnique = true)]
public sealed class ArchiveJobTag
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("job_id")]
    public Guid JobId { get; set; }

    [ForeignKey(nameof(JobId))]
    [InverseProperty(nameof(ArchiveJob.Tags))]
    public ArchiveJob Job { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    [Column("value")]
    public required string Value { get; set; }
}
