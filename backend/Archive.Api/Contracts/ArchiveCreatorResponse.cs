using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts;

[Description("Public profile of the registered user who submitted the archive. Reflects the current handle, display name and stored profile image.")]
public sealed record ArchiveCreatorResponse(
    [property: JsonPropertyName("handle"), Description("Current unique lowercase handle of the user who submitted the archive."), Required, StringLength(32, MinimumLength = 4)] string Handle,
    [property: JsonPropertyName("displayName"), Description("Current public display name of the user who submitted the archive."), Required, StringLength(64, MinimumLength = 6)] string DisplayName,
    [property: JsonPropertyName("profileImageUrl"), Description("Absolute public URL of the submitter's current stored profile image, or null when no image is stored."), StringLength(256)] string? ProfileImageUrl);
