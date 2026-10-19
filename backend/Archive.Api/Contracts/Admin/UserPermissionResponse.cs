using System.ComponentModel;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using Archive.Core.Entities.Authentication.Permissible;

namespace Archive.Api.Contracts.Admin;

/// <summary>Saved evaluation of one permission, including the personal override.</summary>
[Description("Saved evaluation of one permission, including the personal override.")]
public sealed class UserPermissionResponse
{
    [JsonPropertyName("key")]
    [Description("Exact permission key from the catalog.")]
    [StringLength(128)]
    public required string Key { get; init; }

    [JsonPropertyName("defaultAllowed")]
    [Description("Whether the saved role grants this permission by default.")]
    public required bool DefaultAllowed { get; init; }

    [JsonPropertyName("override")]
    [Description("Personal ALLOW or DENY exception. Null means the role default applies.")]
    public required ArchivePermissionEffect? Override { get; init; }

    [JsonPropertyName("allowed")]
    [Description("Effective result for this key only. An operation can require several allowed keys together.")]
    public required bool Allowed { get; init; }
}
