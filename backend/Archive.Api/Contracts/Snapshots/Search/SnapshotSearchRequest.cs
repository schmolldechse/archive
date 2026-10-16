using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Archive.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Archive.Core.Models.Snapshots;
using Archive.Api.Validation;

namespace Archive.Api.Contracts.Snapshots.Search;

[Description("Snapshot search filters and pagination.")]
public sealed class SnapshotSearchRequest
{
    [Description("Whitespace-separated terms. Every term must match the title, description, source URL, or a tag. At most 20 terms are allowed.")]
    [FromQuery(Name = "text")]
    [StringLength(SnapshotValidation.MaxSearchTextLength)]
    public string? Text { get; init; }

    [Description("Exact archived source URL. The value is normalized before matching.")]
    [FromQuery(Name = "sourceUrl")]
    [Url]
    [StringLength(SnapshotValidation.MaxSourceUrlLength)]
    public string? SourceUrl { get; init; }

    [Description("Case-insensitive title substring.")]
    [FromQuery(Name = "title")]
    [StringLength(SnapshotValidation.MaxTitleLength)]
    public string? Title { get; init; }

    [Description("Case-insensitive description substring.")]
    [FromQuery(Name = "description")]
    [StringLength(SnapshotValidation.MaxDescriptionLength)]
    public string? Description { get; init; }

    [Description("Up to 30 different tags, each at most 128 characters. All tags must match. Repeat the query parameter for multiple tags; empty values and duplicates are ignored.")]
    [FromQuery(Name = "tags")]
    public string[]? Tags { get; init; }

    [Description("Source used to create the snapshot.")]
    [FromQuery(Name = "sourceType")]
    [ModelBinder(BinderType = typeof(SnapshotEnumQueryBinder))]
    public SourceType? SourceType { get; init; }

    [Description("Completeness of the snapshot.")]
    [FromQuery(Name = "quality")]
    [ModelBinder(BinderType = typeof(SnapshotEnumQueryBinder))]
    public SnapshotQuality? Quality { get; init; }

    [Description("Inclusive start of the capture range as an offset timestamp.")]
    [FromQuery(Name = "capturedFrom")]
    public DateTimeOffset? CapturedFrom { get; init; }

    [Description("Exclusive end of the capture range as an offset timestamp.")]
    [FromQuery(Name = "capturedUntil")]
    public DateTimeOffset? CapturedUntil { get; init; }

    [Description("Capture-time ordering: desc (newest first, default) or asc (oldest first). Snapshot IDs use the same direction as a stable tie-breaker.")]
    [FromQuery(Name = "order")]
    [ModelBinder(BinderType = typeof(SnapshotEnumQueryBinder))]
    public SnapshotSortOrder Order { get; init; } = SnapshotSortOrder.Desc;

    [Description("One-based result page number.")]
    [FromQuery(Name = "page")]
    [Range(1, SnapshotValidation.MaxPage)]
    public int Page { get; init; } = 1;

    [Description("Number of snapshots per result page.")]
    [FromQuery(Name = "pageSize")]
    [Range(1, SnapshotValidation.MaxPageSize)]
    public int PageSize { get; init; } = SnapshotValidation.DefaultPageSize;
}
