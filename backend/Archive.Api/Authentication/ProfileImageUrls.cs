using Archive.Core.Entities.Authentication;
using Archive.Core.Models.Users;

namespace Archive.Api.Authentication;

internal static class ProfileImageUrls
{
    public static string? GetProfileImageUrl(this ArchiveUser user, string publicApiOrigin) =>
        Build(user.ProfileImage is { Length: > 0 }, publicApiOrigin, "/api/users/profile-image");

    public static string? GetProfileImageUrl(this PublicUser user, string publicApiOrigin) =>
        Build(user.HasProfileImage, publicApiOrigin,
            $"/api/users/{Uri.EscapeDataString(user.Handle)}/profile-image");

    private static string? Build(bool hasImage, string publicApiOrigin, string path) =>
        hasImage ? $"{publicApiOrigin.TrimEnd('/')}{path}" : null;
}
