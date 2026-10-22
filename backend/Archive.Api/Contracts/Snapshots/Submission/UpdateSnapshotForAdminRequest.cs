using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Api.Validation;
using Archive.Core.Models;
using Archive.Core.Models.Snapshots;
using Archive.Core.Models.Users;

namespace Archive.Api.Contracts.Snapshots.Submission;

/// <summary>Complete editable representation of a snapshot and its optional account attribution.</summary>
[Description("Complete editable snapshot fields for administrators. A null createdByHandle removes account attribution.")]
public sealed class UpdateSnapshotForAdminRequest : IValidatableObject
{
    [JsonPropertyName("sourceUrl"), Required(AllowEmptyStrings = false), Url]
    [StringLength(SnapshotValidation.MaxSourceUrlLength)]
    public required string SourceUrl { get; init; }

    [JsonPropertyName("title"), Required(AllowEmptyStrings = false)]
    [StringLength(SnapshotValidation.MaxTitleLength)]
    public required string Title { get; init; }

    [JsonPropertyName("description")]
    [StringLength(SnapshotValidation.MaxDescriptionLength)]
    public string? Description { get; init; }

    [JsonPropertyName("tags")]
    [MaxLength(SnapshotValidation.MaxTags)]
    public string[]? Tags { get; init; }

    [JsonPropertyName("createdByHandle")]
    [StringLength(UserProfileValidation.MaxHandleLength, MinimumLength = UserProfileValidation.MinHandleLength)]
    [Description("Current handle of the desired owner, or null to remove account attribution.")]
    public required string? CreatedByHandle { get; init; }

    public SnapshotAdministrationUpdate ToModel() =>
        new(new SnapshotUpdate(SourceUrl, Title, Description, Tags), CreatedByHandle);

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var model = ToModel();
        foreach (var result in ModelValidation.GetErrors(model.Snapshot).ToValidationResults())
            yield return result;
        foreach (var result in ModelValidation.GetErrors(model).ToValidationResults())
            yield return result;
    }
}
