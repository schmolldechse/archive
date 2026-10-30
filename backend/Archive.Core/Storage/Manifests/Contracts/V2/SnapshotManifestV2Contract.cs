using System.Text.Json.Serialization;
using Archive.Core.Storage.Manifests.Contracts.Legacy;

namespace Archive.Core.Storage.Manifests.Contracts.V2;

/// <summary>Persisted contract for V2 imported captures.</summary>
internal sealed class SnapshotManifestV2Contract : ILegacySnapshotManifestContract
{
    [JsonPropertyName("version")]
    public int Version { get; init; }

    [JsonPropertyName("htmlObjectKey")]
    public string? HtmlObjectKey { get; init; }

    [JsonPropertyName("originalUrl")]
    public string? OriginalUrl { get; init; }

    [JsonPropertyName("resources")]
    public IReadOnlyList<LegacySnapshotResourceContract>? Resources { get; init; }
}
