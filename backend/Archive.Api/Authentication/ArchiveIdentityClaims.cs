using Archive.Api.Exceptions;
using Archive.Core.Entities.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Archive.Api.Authentication;

internal static class ArchiveIdentityClaims
{
    public static Guid ReadUserId(this ClaimsPrincipal? principal)
    {
        if (!Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty)
            throw new InvalidSessionException();

        return userId;
    }

    public static string? GetProfileImageUrl(this ArchiveUser user, string publicApiOrigin)
    {
        if (user.ProfileImage is not { Length: > 0 } image)
            return null;

        var version = Convert.ToHexStringLower(SHA256.HashData(image));
        return $"{publicApiOrigin.TrimEnd('/')}/api/users/profile-image?v={version}";
    }
}
