using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Api.Validation;
using Archive.Core.Models;
using Archive.Core.Models.Snapshots;

namespace Archive.Api.Contracts.Snapshots.Submission;

/// <summary>Complete editable representation of a snapshot owned by the current account.</summary>
[Description("Complete editable snapshot fields. Changing sourceUrl requests another review for owner updates.")]
public sealed class UpdateOwnSnapshotRequest : IValidatableObject
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

    public SnapshotUpdate ToModel() => new(SourceUrl, Title, Description, Tags);

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        ModelValidation.GetErrors(ToModel()).ToValidationResults();
}
