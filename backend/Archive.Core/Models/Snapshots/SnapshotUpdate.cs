using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Archive.Core.Models.Users;

namespace Archive.Core.Models.Snapshots;

/// <summary>Normalized user-editable snapshot fields shared by owner and administrative updates.</summary>
public sealed record SnapshotUpdate : IValidatableObject
{
    [Required]
    [StringLength(SnapshotValidation.MaxSourceUrlLength)]
    public string SourceUrl { get; }

    [Required]
    [StringLength(SnapshotValidation.MaxTitleLength)]
    public string Title { get; }

    [StringLength(SnapshotValidation.MaxDescriptionLength)]
    public string? Description { get; }

    [MaxLength(SnapshotValidation.MaxTags)]
    public IReadOnlyList<string> Tags { get; }

    public SnapshotUpdate(string sourceUrl, string title, string? description, IReadOnlyList<string>? tags)
    {
        SourceUrl = (sourceUrl ?? string.Empty).Trim();
        Title = (title ?? string.Empty).Trim();
        var normalizedDescription = description?.Trim();
        Description = string.IsNullOrWhiteSpace(normalizedDescription) ? null : normalizedDescription;
        Tags = (tags ?? []).Select(SnapshotValidation.NormalizeTag)
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var error in SnapshotValidation.ValidateSourceUrl(SourceUrl, nameof(SourceUrl)))
            yield return error;
        if (Title.Any(char.IsControl))
            yield return new("The title cannot contain control characters.", [nameof(Title)]);
        foreach (var error in SnapshotValidation.ValidateTags(Tags, allowEmpty: false))
            yield return error;
    }
}

/// <summary>Administrative snapshot update including the complete desired owner attribution.</summary>
public sealed record SnapshotAdministrationUpdate(SnapshotUpdate Snapshot, string? CreatedByHandle) : IValidatableObject
{
    public string? CreatedByHandle { get; } = CreatedByHandle is null
        ? null
        : UserProfileValidation.NormalizeHandle(CreatedByHandle);

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CreatedByHandle is null) yield break;
        if (CreatedByHandle.Length is < UserProfileValidation.MinHandleLength or > UserProfileValidation.MaxHandleLength ||
            !Regex.IsMatch(CreatedByHandle, "^[a-z0-9][a-z0-9_-]*[a-z0-9]$", RegexOptions.CultureInvariant))
            yield return new("Choose a valid registered user handle.", [nameof(CreatedByHandle)]);
    }
}

/// <summary>Result of a conditional owner or administrative snapshot update.</summary>
public abstract record SnapshotUpdateResult
{
    private SnapshotUpdateResult() { }

    public sealed record Updated : SnapshotUpdateResult;
    public sealed record Unchanged : SnapshotUpdateResult;
    public sealed record Invalid(IReadOnlyList<FieldValidationError> Errors) : SnapshotUpdateResult;
    public sealed record NotFound : SnapshotUpdateResult;
    public sealed record Forbidden : SnapshotUpdateResult;
    public sealed record Stale : SnapshotUpdateResult;
    public sealed record SourceNotEditable : SnapshotUpdateResult;
}
