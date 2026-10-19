using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Archive.Api.Validation;
using Archive.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Archive.Api.Contracts.Snapshots.Submission;

/// <summary>Bounded private submission search and history pagination.</summary>
public sealed class SnapshotSubmissionQuery
{
    [FromQuery(Name = "q"), StringLength(500)]
    [Description("Title or source URL substring.")]
    public string? Q { get; init; }

    [FromQuery(Name = "moderationStatus"), ModelBinder(BinderType = typeof(SnapshotEnumQueryBinder))]
    [Description("Exact moderation status. Without it, moderation lists show pending and resubmitted captures; owner lists show all submissions.")]
    public SnapshotModerationStatus? ModerationStatus { get; init; }

    [FromQuery(Name = "page"), Range(1, 1_000_000)]
    public int Page { get; init; } = 1;

    [FromQuery(Name = "pageSize"), Range(1, 100)]
    public int PageSize { get; init; } = 25;
}
