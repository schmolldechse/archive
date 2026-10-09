using System.Security.Claims;
using System.Security.Cryptography;
using Archive.Api.Contracts.Authentication;
using Archive.Api.Exceptions;
using Archive.Api.Mapping;
using Archive.Core.Repositories.UserRepository;

namespace Archive.Api.Authentication;

public sealed class ArchiveSessionReader(
    IUserRepository users,
    ArchiveAuthenticationOptions options,
    AuthenticationMapper mapper)
{
    public async Task<AuthenticationSession> ReadAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return new AuthenticationSession { IsAuthenticated = false };

        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty)
            throw new InvalidSessionException();

        var account = await users.GetByIdAsync(userId, cancellationToken);
        if (account is null)
            throw new InvalidSessionException();

        var profileImageUrl = account.ProfileImage is { Length: > 0 } image
            ? $"{options.PublicApiOrigin.TrimEnd('/')}/api/users/me/profile-image?v={Convert.ToHexStringLower(SHA256.HashData(image))}"
            : null;

        return new AuthenticationSession
        {
            IsAuthenticated = true,
            User = mapper.ToResponse(account, profileImageUrl)
        };
    }
}
