using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts.Snapshots.Submission;

/// <summary>A page of general versioned snapshot history.</summary>
[Description("A page of general versioned snapshot history.")]
public sealed class SnapshotHistoryResponse
{
    [JsonPropertyName("items")]
    [Description("History entries ordered from newest to oldest.")]
    public required IReadOnlyList<SnapshotHistoryItemResponse> Items { get; init; }

    [JsonPropertyName("total")]
    [Description("Total number of history entries visible to the current reader.")]
    [Range(0, int.MaxValue)]
    public required int Total { get; init; }

    [JsonPropertyName("page")]
    [Description("Requested one-based page number.")]
    [Range(1, 1_000_000)]
    public required int Page { get; init; }

    [JsonPropertyName("pageSize")]
    [Description("Maximum number of entries in this page.")]
    [Range(1, 100)]
    public required int PageSize { get; init; }
}
