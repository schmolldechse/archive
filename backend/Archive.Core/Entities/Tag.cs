using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Entities;

[Table("tags", Schema = "archive")]
[Index(nameof(Value), IsUnique = true)]
public sealed class Tag
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(128)]
    [Column("value")]
    public required string Value { get; set; }

    [InverseProperty(nameof(SnapshotTag.Tag))]
    public List<SnapshotTag> SnapshotTags { get; set; } = [];
}
