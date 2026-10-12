using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Archive.Api.Contracts.Archive.Creation;

/// <summary>A supported document upload and metadata for a new archive job.</summary>
public sealed class CreateArchiveFileRequest : IValidatableObject
{
    [FromForm(Name = "file")]
    [JsonPropertyName("file")]
    [Description("Non-empty .html, .mhtml or .webarchive file to archive.")]
    [BindRequired]
    [Required]
    public required IFormFile File { get; init; }

    [FromForm(Name = "originalLink")]
    [JsonPropertyName("originalLink")]
    [Description("Original public HTTP or HTTPS URL for the uploaded content.")]
    [BindRequired]
    [Required(AllowEmptyStrings = false, ErrorMessage = "An original page URL is required.")]
    [Url]
    [StringLength(2_048)]
    public required string OriginalLink { get; init; }

    [FromForm(Name = "title")]
    [JsonPropertyName("title")]
    [Description("Title for the snapshot that will be created. Control characters are not permitted.")]
    [BindRequired]
    [Required(AllowEmptyStrings = false, ErrorMessage = "A title is required.")]
    [StringLength(500)]
    public required string Title { get; init; }

    [FromForm(Name = "description")]
    [JsonPropertyName("description")]
    [Description("Optional description for the snapshot.")]
    [StringLength(10_000)]
    public string? Description { get; init; }

    [FromForm(Name = "tags")]
    [JsonPropertyName("tags")]
    [Description("Up to 30 tags of 1 to 100 characters each. Submit each tag as a separate tags form field. Leading hash signs and whitespace are removed; tags are stored in lowercase and duplicates are removed. Empty tags and control characters are not permitted.")]
    [MaxLength(30)]
    public string[]? Tags { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in ArchiveRequestValidation.Validate(OriginalLink, "originalLink", Title, Tags))
            yield return result;

        if (File is { Length: 0 })
            yield return new ValidationResult("Provide a non-empty .html, .mhtml or .webarchive file.", ["file"]);
    }
}
