using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Archive.Core.Models.Snapshots;

namespace Archive.Api.Contracts.Snapshots.Creation;

/// <summary>A supported document upload and metadata for a new snapshot.</summary>
public sealed class CreateSnapshotFileRequest : IValidatableObject
{
    [FromForm(Name = "file")]
    [JsonPropertyName("file")]
    [Description("Non-empty .html, .mhtml or .webarchive file to archive.")]
    [BindRequired]
    [Required]
    public required IFormFile File { get; init; }

    [FromForm(Name = "sourceUrl")]
    [JsonPropertyName("sourceUrl")]
    [Description("Original public HTTP or HTTPS URL for the uploaded content.")]
    [BindRequired]
    [Required(AllowEmptyStrings = false, ErrorMessage = "A source URL is required.")]
    [Url]
    [StringLength(SnapshotValidation.MaxSourceUrlLength)]
    public required string SourceUrl { get; init; }

    [FromForm(Name = "title")]
    [JsonPropertyName("title")]
    [Description("Title for the snapshot that will be created. Control characters are not permitted.")]
    [BindRequired]
    [Required(AllowEmptyStrings = false, ErrorMessage = "A title is required.")]
    [StringLength(SnapshotValidation.MaxTitleLength)]
    public required string Title { get; init; }

    [FromForm(Name = "description")]
    [JsonPropertyName("description")]
    [Description("Optional snapshot description, up to 2,048 characters.")]
    [StringLength(SnapshotValidation.MaxDescriptionLength)]
    public string? Description { get; init; }

    [FromForm(Name = "tags")]
    [JsonPropertyName("tags")]
    [Description("Up to 30 tags of 1 to 128 characters each. Submit each tag as a separate tags form field. Leading hash signs and whitespace are removed; tags are stored in lowercase and duplicates are removed. Empty tags and control characters are not permitted.")]
    [MaxLength(SnapshotValidation.MaxTags)]
    public string[]? Tags { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in SnapshotRequestValidation.Validate(SourceUrl, Title, Description, Tags))
            yield return result;

        if (File is { Length: 0 })
            yield return new ValidationResult("Provide a non-empty .html, .mhtml or .webarchive file.", ["file"]);
    }
}
