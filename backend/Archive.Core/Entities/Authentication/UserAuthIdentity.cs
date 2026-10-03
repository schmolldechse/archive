using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Archive.Core.Entities.Authentication;

[Table("user_auth_identities", Schema = "authentication")]
[Index(nameof(UserId), nameof(AuthProviderId), IsUnique = true)]
[Index(nameof(AuthProviderId), nameof(ExternalSubject), IsUnique = true)]
public sealed class UserAuthIdentity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(ArchiveUser.Identities))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public ArchiveUser User { get; set; } = null!;

    [Column("auth_provider_id")]
    public Guid AuthProviderId { get; set; }

    [ForeignKey(nameof(AuthProviderId))]
    [InverseProperty(nameof(AuthProvider.Identities))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public AuthProvider AuthProvider { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    [Column("external_subject")]
    public required string ExternalSubject { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("last_login_at")]
    public DateTimeOffset LastLoginAt { get; set; } = DateTimeOffset.UtcNow;
}
