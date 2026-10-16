using System.Security.Cryptography;
using Archive.Core.Entities.Authentication;
using Archive.Core.Models.Users;

namespace Archive.Api.Authentication;

internal static class ProfileImageUrls
{
    public static string? GetProfileImageUrl(this ArchiveUser user, string publicApiOrigin) =>
        Build(user.ProfileImage, publicApiOrigin, "/api/users/profile-image");

    public static string? GetProfileImageUrl(this ArchiveCreator creator, string publicApiOrigin) =>
        Build(creator.ProfileImage, publicApiOrigin,
            $"/api/users/{Uri.EscapeDataString(creator.Handle)}/profile-image");

    private static string? Build(byte[]? image, string publicApiOrigin, string path)
    {
        if (image is not { Length: > 0 })
            return null;

        var version = Convert.ToHexStringLower(SHA256.HashData(image));
        return $"{publicApiOrigin.TrimEnd('/')}{path}?v={version}";
    }
}
