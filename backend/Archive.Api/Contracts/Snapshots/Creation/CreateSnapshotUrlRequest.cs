using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Core.Models.Snapshots;

namespace Archive.Api.Contracts.Snapshots.Creation;

[Description("Source URL and metadata for a new snapshot.")]
public sealed class CreateSnapshotUrlRequest : IValidatableObject
{
    [JsonPropertyName("sourceUrl")]
    [Description("Public HTTP or HTTPS URL to archive. The normalized URL must also meet the length limit.")]
    [Required(AllowEmptyStrings = false, ErrorMessage = "A source URL is required.")]
    [Url]
    [StringLength(SnapshotValidation.MaxSourceUrlLength)]
    public required string SourceUrl { get; init; }

    [JsonPropertyName("title")]
    [Description("Snapshot title. Control characters are not permitted.")]
    [Required(AllowEmptyStrings = false, ErrorMessage = "A title is required.")]
    [StringLength(SnapshotValidation.MaxTitleLength)]
    public required string Title { get; init; }

    [JsonPropertyName("description")]
    [Description("Optional snapshot description.")]
    [StringLength(SnapshotValidation.MaxDescriptionLength)]
    public string? Description { get; init; }

    [JsonPropertyName("tags")]
    [Description("Snapshot tags. Leading hash signs and surrounding whitespace are removed; values are stored in lowercase without duplicates. Empty tags and control characters are not permitted.")]
    [MaxLength(SnapshotValidation.MaxTags)]
    public string[]? Tags { get; init; }

    [JsonPropertyName("isPrivate")]
    [Description("Whether the snapshot is visible only to its owner and authorized archive staff. Requires a fully registered account.")]
    public bool IsPrivate { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        SnapshotRequestValidation.Validate(SourceUrl, Title, Description, Tags);
}
