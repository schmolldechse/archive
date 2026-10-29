using System.ComponentModel.DataAnnotations;

namespace Archive.Core.Models.Snapshots;

public sealed record SnapshotCreationRequest(
    [property: Required, StringLength(SnapshotValidation.MaxSourceUrlLength)] string SourceUrl,
    [property: Required, StringLength(SnapshotValidation.MaxTitleLength)] string Title,
    [property: StringLength(SnapshotValidation.MaxDescriptionLength)] string? Description,
    [property: MaxLength(SnapshotValidation.MaxTags)] IReadOnlyList<string>? Tags,
    Guid? CreatedByUserId = null,
    bool IsPrivate = false) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var error in SnapshotValidation.ValidateSourceUrl(SourceUrl, nameof(SourceUrl)))
            yield return error;
        if ((Title?.Trim() ?? string.Empty).Any(char.IsControl))
            yield return new("The title cannot contain control characters.", [nameof(Title)]);
        foreach (var error in SnapshotValidation.ValidateTags(Tags, allowEmpty: false))
            yield return error;
    }
}
public sealed record SnapshotUpload(string FileName, long Length, Func<Stream> OpenReadStream);

public abstract record SnapshotCreationResult
{
    private SnapshotCreationResult() { }
    public sealed record Created(Guid Id, Entities.SnapshotModerationStatus ModerationStatus,
        bool IsPrivate) : SnapshotCreationResult;
    public sealed record Invalid(IReadOnlyList<FieldValidationError> Errors) : SnapshotCreationResult;
    public sealed record UploadTooLarge : SnapshotCreationResult;
    public sealed record AccountUnavailable : SnapshotCreationResult;
    public sealed record PrivateRequiresRegistration : SnapshotCreationResult;
}
