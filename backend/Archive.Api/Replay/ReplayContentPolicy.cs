using Archive.Api.Options;
using Archive.Api.Authentication;
using Archive.Core.Storage;

namespace Archive.Api.Replay;

/// <summary>Builds role-specific response controls for untrusted archived resources.</summary>
public sealed class ReplayContentPolicy(ReplayOptions replay, ArchiveAuthenticationOptions authentication)
{
    private const string DeniedPermissions =
        "accelerometer=(), ambient-light-sensor=(), autoplay=(), bluetooth=(), camera=(), clipboard-read=(), " +
        "clipboard-write=(), display-capture=(), encrypted-media=(), geolocation=(), gyroscope=(), hid=(), " +
        "magnetometer=(), microphone=(), midi=(), payment=(), publickey-credentials-get=(), screen-wake-lock=(), " +
        "serial=(), usb=(), web-share=(), window-management=()";

    public bool IsAllowedDestination(string role, string contentType, string? destination)
    {
        var isDocumentDestination = destination is "document" or "iframe";
        if (role is SnapshotReplayResourceRoles.RootDocument or SnapshotReplayResourceRoles.NestedDocument)
            return isDocumentDestination;

        if (IsActiveAsset(contentType) && isDocumentDestination)
            return false;
        return true;
    }

    public void Apply(HttpResponse response, Guid snapshotId, string role, string contentType, bool interactive)
    {
        var scope = $"{replay.PublicOrigin}/api/snapshots/{snapshotId}/content/";
        response.Headers.Vary = "Sec-CH-UA";
        response.Headers.AccessControlAllowOrigin = "*";
        response.Headers.XContentTypeOptions = "nosniff";
        response.Headers["Referrer-Policy"] = "no-referrer";
        response.Headers["Permissions-Policy"] = DeniedPermissions;

        if (role is SnapshotReplayResourceRoles.RootDocument or SnapshotReplayResourceRoles.NestedDocument)
        {
            response.Headers.CacheControl = "no-store";
            response.Headers.ContentSecurityPolicy = DocumentPolicy(scope,
                role == SnapshotReplayResourceRoles.RootDocument, interactive);
            if (interactive)
            {
                response.Headers["Connection-Allowlist"] = $"(\"{scope}*\")";
            }
            return;
        }

        if (IsActiveAsset(contentType))
            response.Headers.ContentSecurityPolicy =
                "default-src 'none'; script-src 'none'; connect-src 'none'; object-src 'none'; " +
                "form-action 'none'; base-uri 'none'; sandbox";
    }

    private string DocumentPolicy(string scope, bool root, bool interactive)
    {
        var policy = interactive
            ? $"default-src 'none'; script-src {scope} 'unsafe-inline'; style-src {scope} 'unsafe-inline' data:; " +
              $"img-src {scope} data: blob:; font-src {scope} data:; media-src {scope} data: blob:; " +
              $"frame-src {scope} data:; connect-src {scope}; worker-src 'none'; object-src 'none'; " +
              $"form-action 'none'; base-uri {scope}; sandbox allow-scripts;"
            : $"default-src 'none'; img-src {scope} data: blob:; style-src {scope} 'unsafe-inline' data:; " +
              $"font-src {scope} data:; media-src {scope} data: blob:; frame-src {scope} data:; " +
              "script-src 'none'; connect-src 'none'; worker-src 'none'; object-src 'none'; form-action 'none'; " +
              $"base-uri {scope}; sandbox;";

        return root ? policy + $" frame-ancestors {authentication.FrontendOrigin.TrimEnd('/')};" : policy;
    }

    private static bool IsActiveAsset(string contentType) =>
        contentType.StartsWith("image/svg+xml", StringComparison.OrdinalIgnoreCase) ||
        contentType.StartsWith("application/xml", StringComparison.OrdinalIgnoreCase) ||
        contentType.StartsWith("text/xml", StringComparison.OrdinalIgnoreCase) ||
        contentType.StartsWith("application/xhtml+xml", StringComparison.OrdinalIgnoreCase);
}
