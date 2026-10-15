using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts.Snapshots.Creation;

/// <summary>Metadata and source URL for a new snapshot.</summary>
/// <example>{"sourceUrl":"https://example.org/","title":"Example page","description":"A public page captured for reference.","tags":["reference","web"]}</example>
public sealed class CreateSnapshotUrlRequest : IValidatableObject
{
    [JsonPropertyName("sourceUrl")]
    [Description("Public HTTP or HTTPS URL to archive.")]
    [Required(AllowEmptyStrings = false, ErrorMessage = "A source URL is required.")]
    [Url]
    [StringLength(2_048)]
    public required string SourceUrl { get; init; }

    [JsonPropertyName("title")]
    [Description("Title for the snapshot that will be created. Control characters are not permitted.")]
    [Required(AllowEmptyStrings = false, ErrorMessage = "A title is required.")]
    [StringLength(500)]
    public required string Title { get; init; }

    [JsonPropertyName("description")]
    [Description("Optional description for the snapshot.")]
    [StringLength(10_000)]
    public string? Description { get; init; }

    [JsonPropertyName("tags")]
    [Description("Up to 30 tags of 1 to 100 characters each. Leading hash signs and surrounding whitespace are removed; tags are stored in lowercase and duplicates are removed. Empty tags and control characters are not permitted.")]
    [MaxLength(30)]
    public string[]? Tags { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => SnapshotRequestValidation.Validate(SourceUrl, "sourceUrl", Title, Tags);
}
