using Archive.Api.Options;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace Archive.Api.SnapshotContent;

/// <summary>Issues short-lived bearer capabilities for private content on the credential-free snapshot origin.</summary>
public sealed class SnapshotContentAccessTokenService(
    IDataProtectionProvider dataProtection,
    SnapshotContentOptions options)
{
    private readonly ITimeLimitedDataProtector _protector = dataProtection
        .CreateProtector("Archive.Api.SnapshotContent.PrivateAccess.v2")
        .ToTimeLimitedDataProtector();

    /// <summary>Creates a URL-safe capability bound to the authorized account and snapshot.</summary>
    public string Create(Guid snapshotId, Guid subjectUserId) => _protector.Protect(
        $"{snapshotId:N}.{subjectUserId:N}",
        TimeSpan.FromMinutes(options.PrivateAccessTokenLifetimeMinutes));

    /// <summary>Reads the account from an unexpired capability issued for the requested snapshot.</summary>
    public bool TryValidate(Guid snapshotId, string token, out Guid subjectUserId)
    {
        subjectUserId = Guid.Empty;
        try
        {
            var payload = _protector.Unprotect(token, out _).Split('.', StringSplitOptions.RemoveEmptyEntries);
            return payload.Length == 2 &&
                Guid.TryParseExact(payload[0], "N", out var protectedSnapshotId) &&
                Guid.TryParseExact(payload[1], "N", out subjectUserId) &&
                protectedSnapshotId == snapshotId;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
