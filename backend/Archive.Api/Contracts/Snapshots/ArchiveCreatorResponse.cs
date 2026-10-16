using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Core.Models.Users;

namespace Archive.Api.Contracts.Snapshots;

[Description("Public profile of the user who submitted the snapshot.")]
public sealed record ArchiveCreatorResponse
{
    [JsonPropertyName("handle")]
    [Description("Current lowercase handle of the submitting user.")]
    [StringLength(UserProfileValidation.MaxHandleLength, MinimumLength = UserProfileValidation.MinHandleLength)]
    public required string Handle { get; init; }

    [JsonPropertyName("displayName")]
    [Description("Current display name of the submitting user.")]
    [StringLength(UserProfileValidation.MaxDisplayNameLength, MinimumLength = UserProfileValidation.MinDisplayNameLength)]
    public required string DisplayName { get; init; }

    [JsonPropertyName("profileImageUrl")]
    [Description("Public profile image URL. Null when no image is stored.")]
    [Url]
    public required string? ProfileImageUrl { get; init; }
}
