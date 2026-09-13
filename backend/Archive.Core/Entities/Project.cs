using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Entities;

[Table("projects", Schema = "archive")]
[Index(nameof(ProjectKey), IsUnique = true)]
public sealed class Project
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(4_500)]
    [Column("project_key")]
    public required string ProjectKey { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [InverseProperty(nameof(Snapshot.Project))]
    public List<Snapshot> Snapshots { get; set; } = [];
}
