using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Archive.Api.Validation;
using Archive.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Archive.Api.Contracts.Snapshots.Submission;

/// <summary>Bounded administration search for all snapshot records.</summary>
public sealed class SnapshotAdministrationQuery
{
    [FromQuery(Name = "q"), StringLength(500)]
    [Description("Title or source URL substring.")]
    public string? Q { get; init; }

    [FromQuery(Name = "moderationStatus"), ModelBinder(BinderType = typeof(SnapshotEnumQueryBinder))]
    [Description("Exact moderation status.")]
    public SnapshotModerationStatus? ModerationStatus { get; init; }

    [FromQuery(Name = "quality"), ModelBinder(BinderType = typeof(SnapshotEnumQueryBinder))]
    [Description("Capture completeness. COMPLETE selects stored complete captures; INCOMPLETE selects captures with missing resources or snapshots without a completed capture.")]
    public SnapshotQuality? Quality { get; init; }

    [FromQuery(Name = "page"), Range(1, 1_000_000)]
    [Description("One-based page number.")]
    public int Page { get; init; } = 1;

    [FromQuery(Name = "pageSize"), Range(1, 100)]
    [Description("Maximum records per page.")]
    public int PageSize { get; init; } = 25;
}
