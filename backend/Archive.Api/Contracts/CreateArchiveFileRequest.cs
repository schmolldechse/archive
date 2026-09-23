using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Archive.Api.Contracts;

public sealed class CreateArchiveFileRequest
{
    [FromForm(Name = "file")]
    [JsonPropertyName("file")]
    [Description("Non-empty .html, .mhtml or .webarchive file to archive.")]
    [BindRequired]
    [Required]
    public required IFormFile File { get; init; }

    [FromForm(Name = "originalLink")]
    [JsonPropertyName("originalLink")]
    [Description("Optional original public URL for the uploaded content.")]
    [Url]
    [StringLength(2_048)]
    public string? OriginalLink { get; init; }

    [FromForm(Name = "title")]
    [JsonPropertyName("title")]
    [Description("Title for the snapshot that will be created.")]
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
    [Description("Tags assigned to the snapshot. Submit each tag as a separate form field.")]
    [MaxLength(30)]
    public string[]? Tags { get; init; }
}
