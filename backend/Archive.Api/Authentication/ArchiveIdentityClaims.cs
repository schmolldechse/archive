using Archive.Api.Exceptions;
using System.Security.Claims;

namespace Archive.Api.Authentication;

internal static class ArchiveIdentityClaims
{
    public static Guid ReadUserId(this ClaimsPrincipal? principal)
    {
        if (!Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty)
            throw new InvalidSessionException();

        return userId;
    }
}
