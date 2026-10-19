using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Core.Entities.Authentication.Permissible;

namespace Archive.Api.Contracts.Admin;

/// <summary>One explicit exception change; effect must be present, including when null.</summary>
[Description("One personal permission override change. A null effect removes the stored exception.")]
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class PermissionOverrideRequest
{
    [JsonPropertyName("permission")]
    [Description("Known case-sensitive permission key. Each key may occur only once per update.")]
    [Required(AllowEmptyStrings = false)]
    [StringLength(128)]
    public required string Permission { get; init; }

    [JsonPropertyName("effect")]
    [Description("ALLOW grants, DENY blocks, null removes the personal exception. Required property.")]
    public required ArchivePermissionEffect? Effect { get; init; }
}
