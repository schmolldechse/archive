using System.ComponentModel.DataAnnotations;
using Archive.Core.Jobs;

namespace Archive.Core.Models.Snapshots;

public static class SnapshotValidation
{
    public const int MaxTitleLength = 512;
    public const int MaxDescriptionLength = 2_048;
    public const int MaxSourceUrlLength = 4_096;
    public const int MaxTagLength = 128;
    public const int MaxTags = 30;
    public const int MaxSearchTextLength = 500;
    public const int MaxSearchTerms = 20;
    public const int MaxPage = 1_000_000;
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 24;

    public static string NormalizeTag(string? value) =>
        (value ?? string.Empty).Trim().TrimStart('#').ToLowerInvariant();

    public static IEnumerable<ValidationResult> ValidateSourceUrl(string? value, string field)
    {
        if (!UrlNormalizer.TryNormalizeSource(value, out var normalized, out var error))
            yield return new(error, [field]);
        else if (normalized.Length > MaxSourceUrlLength)
            yield return new($"The normalized source URL must contain at most {MaxSourceUrlLength} characters.", [field]);
    }

    public static IEnumerable<ValidationResult> ValidateTags(IEnumerable<string>? values, bool allowEmpty)
    {
        foreach (var value in values ?? [])
        {
            var tag = NormalizeTag(value);
            if (allowEmpty && tag.Length == 0) continue;
            if (value?.Length > MaxTagLength || tag.Length > MaxTagLength || tag.Length == 0)
            {
                yield return new($"Each tag must contain between 1 and {MaxTagLength} characters.", [nameof(SnapshotCreationRequest.Tags)]);
                yield break;
            }
            if (tag.Any(char.IsControl))
            {
                yield return new("Tags cannot contain control characters.", [nameof(SnapshotCreationRequest.Tags)]);
                yield break;
            }
        }
    }
}
