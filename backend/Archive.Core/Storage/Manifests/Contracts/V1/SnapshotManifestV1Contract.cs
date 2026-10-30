using System.Text.Json.Serialization;
using Archive.Core.Storage.Manifests.Contracts.Legacy;

namespace Archive.Core.Storage.Manifests.Contracts.V1;

/// <summary>Persisted contract for V1 URL captures.</summary>
internal sealed class SnapshotManifestV1Contract : ILegacySnapshotManifestContract
{
    [JsonPropertyName("version")]
    public int Version { get; init; }

    [JsonPropertyName("htmlObjectKey")]
    public string? HtmlObjectKey { get; init; }

    [JsonPropertyName("resources")]
    public IReadOnlyList<LegacySnapshotResourceContract>? Resources { get; init; }
}
