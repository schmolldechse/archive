using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts.Admin;

/// <summary>Supported permission definitions in stable catalog order.</summary>
[Description("Supported permission definitions in stable catalog order.")]
public sealed class PermissionCatalogResponse
{
    [JsonPropertyName("items")]
    [Description("All supported permission definitions, without publishing role defaults.")]
    public required IReadOnlyList<PermissionDefinitionResponse> Items { get; init; }
}
