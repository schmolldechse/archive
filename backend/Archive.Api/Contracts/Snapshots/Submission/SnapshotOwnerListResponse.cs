using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Api.Contracts.Users;

namespace Archive.Api.Contracts.Snapshots.Submission;

/// <summary>A handle-ordered page of public user contracts eligible for snapshot attribution.</summary>
[Description("Registered accounts eligible for snapshot attribution. Internal user identifiers are never returned.")]
public sealed class SnapshotOwnerListResponse
{
    [JsonPropertyName("items")]
    public required IReadOnlyList<PublicUserResponse> Items { get; init; }

    [JsonPropertyName("total"), Range(0, int.MaxValue)]
    public required int Total { get; init; }

    [JsonPropertyName("page"), Range(1, 10_000)]
    public required int Page { get; init; }

    [JsonPropertyName("pageSize"), Range(1, 25)]
    public required int PageSize { get; init; }
}
