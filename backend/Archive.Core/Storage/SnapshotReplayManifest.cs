using System.Text.Json.Serialization;

namespace Archive.Core.Storage;

/// <summary>Versioned snapshot contract used to authorize stored resources at the public content boundary.</summary>
public sealed class SnapshotReplayManifest
{
    public const int CurrentManifestVersion = 3;
    public const int CurrentReplayVersion = 1;

    [JsonPropertyName("version")]
    public int Version { get; init; }

    [JsonPropertyName("provenance")]
    public required SnapshotReplayProvenance Provenance { get; init; }

    [JsonPropertyName("replay")]
    public required SnapshotReplayMetadata Replay { get; init; }

    [JsonPropertyName("resources")]
    public required IReadOnlyList<SnapshotReplayResource> Resources { get; init; }

    [JsonPropertyName("missingUrls")]
    public IReadOnlyList<string> MissingUrls { get; init; } = [];

    [JsonPropertyName("missingUrlCount")]
    public int MissingUrlCount { get; init; }

    [JsonPropertyName("incomplete")]
    public bool Incomplete { get; init; }
}

/// <summary>Immutable source and derived-document provenance retained for a capture.</summary>
public sealed class SnapshotReplayProvenance
{
    [JsonPropertyName("derivedDisplayDocument")]
    public bool DerivedDisplayDocument { get; init; }

    [JsonPropertyName("sourceObjectKey")]
    public required string SourceObjectKey { get; init; }

    [JsonPropertyName("sourceSha256")]
    public required string SourceSha256 { get; init; }

    [JsonPropertyName("indexSha256")]
    public required string IndexSha256 { get; init; }

    [JsonPropertyName("sourceUrl")]
    public string? SourceUrl { get; init; }

    [JsonPropertyName("sourceMimeType")]
    public required string SourceMimeType { get; init; }

    [JsonPropertyName("sourceStatusCode")]
    public int? SourceStatusCode { get; init; }

    [JsonPropertyName("capturedAt")]
    public DateTimeOffset CapturedAt { get; init; }

    [JsonPropertyName("documentState")]
    public required string DocumentState { get; init; }

    [JsonPropertyName("redirectChain")]
    public IReadOnlyList<string> RedirectChain { get; init; } = [];
}

/// <summary>Replay compatibility and diagnostics for the canonical entry document.</summary>
public sealed class SnapshotReplayMetadata
{
    [JsonPropertyName("version")]
    public int? Version { get; init; }

    [JsonPropertyName("normalizer")]
    public required string Normalizer { get; init; }

    [JsonPropertyName("containsExecutableContent")]
    public bool ContainsExecutableContent { get; init; }

    [JsonPropertyName("diagnostic")]
    public string? Diagnostic { get; init; }
}

/// <summary>A manifest-authorized object and the browser role it may fulfill.</summary>
public sealed class SnapshotReplayResource
{
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("sourceUrl")]
    public string? SourceUrl { get; init; }

    [JsonPropertyName("contentType")]
    public required string ContentType { get; init; }

    [JsonPropertyName("objectKey")]
    public required string ObjectKey { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }

    [JsonPropertyName("role")]
    public required string Role { get; init; }
}

/// <summary>Stable resource-role names persisted in snapshot manifests.</summary>
public static class SnapshotReplayResourceRoles
{
    public const string RootDocument = "root-document";
    public const string NestedDocument = "nested-document";
    public const string Asset = "asset";

    /// <summary>Infers the conservative browser role used while upgrading older static snapshot generations.</summary>
    public static string Infer(string path, string contentType)
    {
        if (string.Equals(path, "index.html", StringComparison.OrdinalIgnoreCase))
            return RootDocument;
        if (contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) ||
            contentType.StartsWith("application/xhtml+xml", StringComparison.OrdinalIgnoreCase))
            return NestedDocument;
        return Asset;
    }
}
