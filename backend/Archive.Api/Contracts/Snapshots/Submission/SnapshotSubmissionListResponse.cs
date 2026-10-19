using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts.Snapshots.Submission;

/// <summary>A bounded page of private snapshot submissions.</summary>
[Description("A bounded page of private snapshot submissions.")]
public sealed class SnapshotSubmissionListResponse
{
    [JsonPropertyName("items")]
    [Description("Submissions in the requested page.")]
    public required IReadOnlyList<SnapshotSubmissionSummaryResponse> Items { get; init; }

    [JsonPropertyName("total")]
    [Description("Total number of matching submissions.")]
    [Range(0, int.MaxValue)]
    public required int Total { get; init; }

    [JsonPropertyName("page")]
    [Description("Requested one-based page number.")]
    [Range(1, 1_000_000)]
    public required int Page { get; init; }

    [JsonPropertyName("pageSize")]
    [Description("Maximum number of submissions in this page.")]
    [Range(1, 100)]
    public required int PageSize { get; init; }
}
