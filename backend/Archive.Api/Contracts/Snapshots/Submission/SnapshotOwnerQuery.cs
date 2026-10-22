using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Archive.Api.Contracts.Snapshots.Submission;

/// <summary>Bounded search for registered accounts that can own a snapshot.</summary>
public sealed class SnapshotOwnerQuery
{
    [FromQuery(Name = "q"), StringLength(128)]
    [Description("Case-insensitive handle or display-name substring.")]
    public string? Q { get; init; }

    [FromQuery(Name = "page"), Range(1, 10_000)]
    public int Page { get; init; } = 1;

    [FromQuery(Name = "pageSize"), Range(1, 25)]
    public int PageSize { get; init; } = 10;
}
