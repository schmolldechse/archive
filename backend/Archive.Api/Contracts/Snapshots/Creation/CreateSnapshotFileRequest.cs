using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Archive.Core.Models.Snapshots;

namespace Archive.Api.Contracts.Snapshots.Creation;

[Description("An HTML, MHTML or Webarchive upload and its snapshot metadata.")]
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
    [Description("Original HTTP or HTTPS URL of the uploaded page. The normalized URL must also meet the length limit.")]
    [BindRequired]
    [Required(AllowEmptyStrings = false, ErrorMessage = "A source URL is required.")]
    [Url]
    [StringLength(SnapshotValidation.MaxSourceUrlLength)]
    public required string SourceUrl { get; init; }

    [FromForm(Name = "title")]
    [JsonPropertyName("title")]
    [Description("Snapshot title. Control characters are not permitted.")]
    [BindRequired]
    [Required(AllowEmptyStrings = false, ErrorMessage = "A title is required.")]
    [StringLength(SnapshotValidation.MaxTitleLength)]
    public required string Title { get; init; }

    [FromForm(Name = "description")]
    [JsonPropertyName("description")]
    [Description("Optional snapshot description.")]
    [StringLength(SnapshotValidation.MaxDescriptionLength)]
    public string? Description { get; init; }

    [FromForm(Name = "tags")]
    [JsonPropertyName("tags")]
    [Description("Snapshot tags, submitted as repeated tags fields. Leading hash signs and surrounding whitespace are removed; values are stored in lowercase without duplicates. Empty tags and control characters are not permitted.")]
    [MaxLength(SnapshotValidation.MaxTags)]
    public string[]? Tags { get; init; }

    [FromForm(Name = "isPrivate")]
    [JsonPropertyName("isPrivate")]
    [Description("Whether the snapshot is visible only to its owner and authorized archive staff. Requires a fully registered account.")]
    public bool IsPrivate { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in SnapshotRequestValidation.Validate(SourceUrl, Title, Description, Tags))
            yield return result;

        if (File is { Length: 0 })
            yield return new ValidationResult("Provide a non-empty .html, .mhtml or .webarchive file.", ["file"]);
    }
}
