using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Archive.Core.Entities.Authentication;

[Table("auth_providers", Schema = "authentication")]
[Index(nameof(ProviderKey), IsUnique = true)]
public sealed class AuthProvider
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(64)]
    [Column("provider_key")]
    public required string ProviderKey { get; set; }

    [Required]
    [MaxLength(64)]
    [Column("display_name")]
    public required string DisplayName { get; set; }

    [InverseProperty(nameof(UserAuthIdentity.AuthProvider))]
    public ICollection<UserAuthIdentity> Identities { get; set; } = [];
}
