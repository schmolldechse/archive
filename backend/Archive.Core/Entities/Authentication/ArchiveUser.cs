using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Archive.Core.Entities.Authentication.Permissible;
using Archive.Core.Models.Users;

namespace Archive.Core.Entities.Authentication;

[Table("users", Schema = "authentication")]
public sealed class ArchiveUser
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(UserProfileValidation.MaxDisplayNameLength)]
    [Column("display_name")]
    public string? DisplayName { get; set; }

    [MaxLength(UserProfileValidation.MaxHandleLength)]
    [Column("handle")]
    public string? Handle { get; set; }

    [Column("profile_image", TypeName = "bytea")]
    public byte[]? ProfileImage { get; set; }

    [Required]
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Required]
    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("registration_completed_at")]
    public DateTimeOffset? RegistrationCompletedAt { get; set; }

    [NotMapped]
    public bool IsRegistrationComplete => RegistrationCompletedAt.HasValue;

    [Required]
    [Column("role")]
    public ArchiveRole Role { get; set; } = ArchiveRole.User;

    [InverseProperty(nameof(UserAuthIdentity.User))]
    public ICollection<UserAuthIdentity> Identities { get; set; } = [];

    [InverseProperty(nameof(ArchiveUserPermissionOverride.User))]
    public ICollection<ArchiveUserPermissionOverride> PermissionOverrides { get; set; } = [];
}
