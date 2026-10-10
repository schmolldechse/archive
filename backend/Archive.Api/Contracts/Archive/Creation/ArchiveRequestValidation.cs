using System.ComponentModel.DataAnnotations;
using Archive.Core.Jobs;

namespace Archive.Api.Contracts.Archive.Creation;

internal static class ArchiveRequestValidation
{
    public static IEnumerable<ValidationResult> Validate(string? sourceUrl, string urlField, string? title, IEnumerable<string>? tags)
    {
        if (!UrlNormalizer.TryNormalizeSource(sourceUrl, out _, out var error))
            yield return new ValidationResult(error, [urlField]);

        if (title is not null && title.Trim().Any(char.IsControl))
            yield return new ValidationResult("The title cannot contain control characters.", ["title"]);

        if (tags is null)
            yield break;

        foreach (var value in tags)
        {
            var tag = NormalizeTag(value);
            if (tag.Length is 0 or > 100)
            {
                yield return new ValidationResult("Each tag must contain between 1 and 100 characters.", ["tags"]);
                yield break;
            }

            if (tag.Any(char.IsControl))
            {
                yield return new ValidationResult("Tags cannot contain control characters.", ["tags"]);
                yield break;
            }
        }
    }

    public static string NormalizeTag(string tag) => (tag ?? string.Empty).Trim().TrimStart('#').ToLowerInvariant();
}
