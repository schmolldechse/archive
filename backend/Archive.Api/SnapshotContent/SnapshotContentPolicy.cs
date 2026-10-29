using Archive.Api.Options;
using Archive.Api.Authentication;
using Archive.Core.Storage;

namespace Archive.Api.SnapshotContent;

/// <summary>Builds browser-independent response controls for untrusted archived resources.</summary>
public sealed class SnapshotContentPolicy(ArchiveAuthenticationOptions authentication)
{
    private const string DeniedPermissions =
        "accelerometer=(), autoplay=(), bluetooth=(), camera=(), clipboard-read=(), " +
        "clipboard-write=(), display-capture=(), encrypted-media=(), geolocation=(), gyroscope=(), hid=(), " +
        "magnetometer=(), microphone=(), midi=(), payment=(), publickey-credentials-get=(), screen-wake-lock=(), " +
        "serial=(), usb=(), web-share=(), window-management=()";

    public bool IsAllowedDestination(string role, string contentType, string? destination)
    {
        if (destination is "serviceworker" or "sharedworker")
            return false;

        var isDocumentDestination = destination is "document" or "iframe";
        if (role is SnapshotResourceRoles.RootDocument or SnapshotResourceRoles.NestedDocument)
            return isDocumentDestination;

        if (IsActiveAsset(contentType) && isDocumentDestination)
            return false;
        return true;
    }

    public void Apply(HttpResponse response, string scope, string role, string contentType)
    {
        response.Headers.AccessControlAllowOrigin = "*";
        response.Headers.XContentTypeOptions = "nosniff";
        response.Headers["X-DNS-Prefetch-Control"] = "off";
        response.Headers["Referrer-Policy"] = "no-referrer";
        response.Headers["Permissions-Policy"] = DeniedPermissions;

        if (role is SnapshotResourceRoles.RootDocument or SnapshotResourceRoles.NestedDocument)
        {
            response.Headers.CacheControl = "no-store";
            response.Headers.ContentSecurityPolicy = DocumentPolicy(scope,
                role == SnapshotResourceRoles.RootDocument);
            response.Headers["Connection-Allowlist"] = $"(\"{scope}*\")";
            return;
        }

        if (IsActiveAsset(contentType))
            response.Headers.ContentSecurityPolicy =
                "default-src 'none'; script-src 'none'; connect-src 'none'; object-src 'none'; " +
                "form-action 'none'; base-uri 'none'; sandbox";
    }

    private string DocumentPolicy(string scope, bool root)
    {
        var policy = $"default-src 'none'; script-src {scope} 'unsafe-inline' 'wasm-unsafe-eval'; " +
            $"style-src {scope} 'unsafe-inline' data:; " +
            $"img-src {scope} data: blob:; font-src {scope} data:; media-src {scope} data: blob:; " +
            $"frame-src {scope}; connect-src {scope} blob: data:; worker-src blob:; object-src 'none'; " +
            $"form-action 'none'; base-uri {scope}; sandbox allow-scripts;";

        return root ? policy + $" frame-ancestors {authentication.FrontendOrigin.TrimEnd('/')};" : policy;
    }

    private static bool IsActiveAsset(string contentType) =>
        contentType.StartsWith("image/svg+xml", StringComparison.OrdinalIgnoreCase) ||
        contentType.StartsWith("application/xml", StringComparison.OrdinalIgnoreCase) ||
        contentType.StartsWith("text/xml", StringComparison.OrdinalIgnoreCase) ||
        contentType.StartsWith("application/xhtml+xml", StringComparison.OrdinalIgnoreCase);
}
