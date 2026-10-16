using Archive.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace Archive.Core.Models.Snapshots;

public sealed record SnapshotSearch(
    [property: StringLength(SnapshotValidation.MaxSearchTextLength)] string? Text = null,
    [property: StringLength(SnapshotValidation.MaxSourceUrlLength)] string? SourceUrl = null,
    [property: StringLength(SnapshotValidation.MaxTitleLength)] string? Title = null,
    [property: StringLength(SnapshotValidation.MaxDescriptionLength)] string? Description = null,
    IReadOnlyList<string>? Tags = null, SourceType? SourceType = null,
    SnapshotQuality? Quality = null, DateTimeOffset? CapturedFrom = null, DateTimeOffset? CapturedUntil = null,
    SnapshotSortOrder Order = SnapshotSortOrder.Desc,
    [property: Range(1, 1_000_000)] int Page = 1,
    [property: Range(1, 100)] int PageSize = 24) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CapturedFrom is not null && CapturedUntil is not null && CapturedFrom >= CapturedUntil)
            yield return new("The end of the capture range must be later than its start.", ["capturedUntil"]);
        if (!string.IsNullOrWhiteSpace(SourceUrl))
            foreach (var error in SnapshotValidation.ValidateSourceUrl(SourceUrl, "sourceUrl"))
                yield return error;
        if ((Text ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length > SnapshotValidation.MaxSearchTerms)
            yield return new($"The text search accepts at most {SnapshotValidation.MaxSearchTerms} terms.", ["text"]);
        var tags = (Tags ?? []).Select(SnapshotValidation.NormalizeTag)
            .Where(tag => tag.Length > 0).Distinct(StringComparer.Ordinal).ToArray();
        if (tags.Length > SnapshotValidation.MaxTags)
            yield return new($"At most {SnapshotValidation.MaxTags} tags can be required.", ["tags"]);
        foreach (var error in SnapshotValidation.ValidateTags(Tags, allowEmpty: true))
            yield return error;
        if (!Enum.IsDefined(Order))
            yield return new("Capture order must be asc or desc.", ["order"]);
    }
}
