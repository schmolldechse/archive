using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Core.Models.Snapshots;
using Archive.Core.Storage;

namespace Archive.Api.Contracts.Snapshots;

/// <summary>Deterministic first-discovery hierarchy of captured snapshot documents.</summary>
[Description("Captured pages in deterministic breadth-first discovery order.")]
public sealed class SnapshotPageTreeResponse
{
    [JsonPropertyName("rootPath")]
    [Description("Snapshot-local path of the root document.")]
    [Required, StringLength(1024)]
    public required string RootPath { get; init; }

    [JsonPropertyName("pages")]
    [Description("Captured documents and their first-discovery parents.")]
    [MinLength(1), MaxLength(SnapshotManifestReader.MaxPageTreePages)]
    public required IReadOnlyList<SnapshotPageResponse> Pages { get; init; }
}

/// <summary>One captured page in a snapshot discovery tree.</summary>
[Description("One captured document and its first-discovery parent.")]
public sealed class SnapshotPageResponse
{
    [JsonPropertyName("path")]
    [Description("Snapshot-local document path and stable node identity.")]
    [Required, StringLength(1024)]
    public required string Path { get; init; }

    [JsonPropertyName("parentPath")]
    [Description("Snapshot-local path of the first-discovery parent. Null only for the root document.")]
    [StringLength(1024)]
    public required string? ParentPath { get; init; }

    [JsonPropertyName("sourceUrl")]
    [Description("Canonical final HTTP or HTTPS URL captured for this document.")]
    [Required, Url, StringLength(SnapshotValidation.MaxSourceUrlLength)]
    public required string SourceUrl { get; init; }

    [JsonPropertyName("title")]
    [Description("Captured document title. Null when the document supplied no title.")]
    [StringLength(SnapshotManifestReader.MaxPageTitleLength)]
    public required string? Title { get; init; }
}
