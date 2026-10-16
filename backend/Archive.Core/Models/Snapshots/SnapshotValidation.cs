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
                yield return new($"Each tag must contain between 1 and {MaxTagLength} characters.", ["tags"]);
                yield break;
            }
            if (tag.Any(char.IsControl))
            {
                yield return new("Tags cannot contain control characters.", ["tags"]);
                yield break;
            }
        }
    }

    public static IReadOnlyList<FieldValidationError> GetErrors(IValidatableObject model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results.SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty)
            .Select(field => new FieldValidationError(
                field.Length == 0 ? field : char.ToLowerInvariant(field[0]) + field[1..],
                result.ErrorMessage ?? "The value is invalid."))).ToArray();
    }
}
