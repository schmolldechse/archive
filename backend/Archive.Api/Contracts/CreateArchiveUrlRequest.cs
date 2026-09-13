using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts;

public sealed class CreateArchiveUrlRequest
{
    [JsonPropertyName("sourceUrl")]
    [Description("Public HTTP or HTTPS URL to archive.")]
    [Required(AllowEmptyStrings = false, ErrorMessage = "A source URL is required.")]
    [Url]
    [StringLength(2_048)]
    public required string SourceUrl { get; init; }

    [JsonPropertyName("title")]
    [Description("Title for the snapshot that will be created.")]
    [Required(AllowEmptyStrings = false, ErrorMessage = "A title is required.")]
    [StringLength(500)]
    public required string Title { get; init; }

    [JsonPropertyName("description")]
    [Description("Optional description for the snapshot.")]
    [StringLength(10_000)]
    public string? Description { get; init; }

    [JsonPropertyName("tags")]
    [Description("Tags assigned to the snapshot.")]
    [MaxLength(30)]
    public string[]? Tags { get; init; }
}
