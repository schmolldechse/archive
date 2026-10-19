using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Archive.Core.Entities.Authentication.Permissible;

[Table("user_permission_overrides", Schema = "authentication")]
[PrimaryKey(nameof(UserId), nameof(Permission))]
public sealed class ArchiveUserPermissionOverride
{
    [Column("user_id")]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(ArchiveUser.PermissionOverrides))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public ArchiveUser User { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    [Column("permission")]
    public required string Permission { get; set; }

    [Required]
    [Column("effect")]
    public required ArchivePermissionEffect Effect { get; set; }
}
