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
        .CreateProtector("Archive.Api.SnapshotContent.PrivateAccess.v1")
        .ToTimeLimitedDataProtector();

    /// <summary>Creates a URL-safe capability after the caller has authorized access to the snapshot.</summary>
    public string Create(Guid snapshotId) => _protector.Protect(snapshotId.ToString("N"),
        TimeSpan.FromMinutes(options.PrivateAccessTokenLifetimeMinutes));

    /// <summary>Validates that an unexpired capability was issued for the requested snapshot.</summary>
    public bool IsValid(Guid snapshotId, string token)
    {
        try
        {
            var protectedSnapshotId = _protector.Unprotect(token, out _);
            return Guid.TryParseExact(protectedSnapshotId, "N", out var parsed) && parsed == snapshotId;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
