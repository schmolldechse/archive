using System.ComponentModel;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Archive.Api.Contracts.Admin;

/// <summary>A supported permission without internal role defaults.</summary>
[Description("A supported permission without internal role defaults.")]
public sealed class PermissionDefinitionResponse
{
    [JsonPropertyName("key")]
    [Description("Exact, case-sensitive permission key.")]
    [StringLength(128)]
    public required string Key { get; init; }

    [JsonPropertyName("displayName")]
    [Description("English display name from the permission catalog.")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("description")]
    [Description("Scope, behavior, and prerequisites of the permission.")]
    public required string Description { get; init; }
}
