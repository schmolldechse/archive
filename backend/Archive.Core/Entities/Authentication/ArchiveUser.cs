using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Entities.Authentication;

[Table("users", Schema = "authentication")]
[Index(nameof(Handle), IsUnique = true)]
public sealed class ArchiveUser
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(64)]
    [Column("display_name")]
    public required string DisplayName { get; set; }

    [Required]
    [MaxLength(32)]
    [Column("handle")]
    public required string Handle { get; set; }

    [Column("profile_image", TypeName = "bytea")]
    public byte[]? ProfileImage { get; set; }

    [Required]
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Required]
    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    [InverseProperty(nameof(UserAuthIdentity.User))]
    public ICollection<UserAuthIdentity> Identities { get; set; } = [];
}
