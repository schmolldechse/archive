using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Archive.Api.Snapshots;

/// <summary>Issues short-lived, partitioned snapshot-scoped cookies for private preview resources.</summary>
public sealed class SnapshotPreviewTicketService
{
    public const string CookieName = "archive.preview.v2";

    private static readonly TimeSpan TicketLifetime = TimeSpan.FromMinutes(15);
    private readonly IDataProtector _protector;

    /// <summary>Creates a ticket protector using the API's shared data-protection key ring.</summary>
    public SnapshotPreviewTicketService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Archive.Api.SnapshotPreviewTicket.v2");
    }

    /// <summary>Issues a cookie restricted to this snapshot's content route and preview scope.</summary>
    public void IssueCookie(HttpResponse response, Guid snapshotId, SnapshotPreviewScope scope)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(TicketLifetime).ToUnixTimeSeconds();
        var payload = $"{snapshotId:N}|{scope}|{expiresAt.ToString(CultureInfo.InvariantCulture)}";
        var ticket = _protector.Protect(payload);
        var options = new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            MaxAge = TicketLifetime,
            Path = ContentPath(snapshotId, scope),
            SameSite = SameSiteMode.None,
            Secure = true
        };
        // Keep third-party embeddings from reusing a preview ticket across top-level sites.
        options.Extensions.Add("Partitioned");
        response.Cookies.Append(CookieName, ticket, options);
    }

    /// <summary>Checks that a preview cookie is valid, unexpired and scoped to this snapshot.</summary>
    public bool IsValid(string? ticket, Guid snapshotId, SnapshotPreviewScope scope)
    {
        if (string.IsNullOrWhiteSpace(ticket))
            return false;

        try
        {
            var payload = _protector.Unprotect(ticket);
            var parts = payload.Split('|', 3);
            return parts.Length == 3 &&
                   Guid.TryParseExact(parts[0], "N", out var protectedSnapshotId) &&
                   protectedSnapshotId == snapshotId &&
                   Enum.TryParse<SnapshotPreviewScope>(parts[1], ignoreCase: false, out var protectedScope) &&
                   protectedScope == scope &&
                   long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var expiresAt) &&
                   expiresAt > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException or ArgumentException)
        {
            return false;
        }
    }

    private static string ContentPath(Guid snapshotId, SnapshotPreviewScope scope)
    {
        var route = scope switch
        {
            SnapshotPreviewScope.Owner => "users",
            SnapshotPreviewScope.Admin => "admin",
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unknown snapshot preview scope.")
        };
        return $"/api/{route}/snapshots/{snapshotId:D}/content";
    }
}
