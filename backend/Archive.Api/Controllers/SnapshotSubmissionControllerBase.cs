using Archive.Api.Authentication;
using Archive.Api.Contracts.Snapshots.Submission;
using Archive.Api.Mapping;
using Archive.Api.Snapshots;
using Archive.Core.Models.Snapshots;
using Archive.Core.Repositories.SnapshotSubmissionRepository;
using Archive.Core.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Archive.Api.Controllers;

/// <summary>Shared private response and conditional-write handling for owners and moderators.</summary>
public abstract class SnapshotSubmissionControllerBase(ISnapshotSubmissionRepository submissions,
    SnapshotSubmissionMapper mapper, ArchiveAuthenticationOptions options,
    SnapshotPreviewTicketService previewTickets) : ControllerBase
{
    protected ISnapshotSubmissionRepository Submissions => submissions;
    protected SnapshotSubmissionMapper Mapper => mapper;
    protected SnapshotPreviewTicketService PreviewTickets => previewTickets;

    protected ActionResult<SnapshotSubmissionDetailResponse> Detail(SnapshotSubmissionDetail? detail, string routeBase)
    {
        if (detail is null) return NotFound();
        Response.Headers.ETag = detail.ETag;
        var resourceBaseUrl = $"{options.PublicApiOrigin.TrimEnd('/')}/{routeBase.Trim('/')}/{detail.Snapshot.Id}";
        return Ok(mapper.ToResponse(detail, resourceBaseUrl));
    }

    /// <summary>Checks browser origin and exactly one strong validator before a private mutation.</summary>
    protected ObjectResult? ValidateWrite(out string tag)
    {
        tag = "";
        if (!string.Equals(Request.Headers.Origin.ToString(), options.FrontendOrigin.TrimEnd('/'), StringComparison.Ordinal))
            return SnapshotProblem(403, "Request origin not allowed", "Use the configured frontend origin.", "request_origin_not_allowed");
        var values = Request.Headers.IfMatch;
        if (values.Count == 0) return SnapshotProblem(428, "Precondition required", "Load the current snapshot before saving.", "if_match_required");
        if (values.Count != 1 || !EntityTagHeaderValue.TryParse(values[0], out var value) || value.IsWeak || value.Tag.Value == "*")
            return SnapshotProblem(400, "Invalid precondition", "Provide exactly one strong ETag in If-Match.", "invalid_if_match");
        tag = value.ToString();
        return null;
    }

    protected ActionResult<SnapshotSubmissionDetailResponse> Outcome(SnapshotReviewOutcome outcome) => outcome switch
    {
        SnapshotReviewOutcome.NotFound => NotFound(),
        SnapshotReviewOutcome.Forbidden => Forbid(),
        SnapshotReviewOutcome.Stale => SnapshotProblem(412, "Snapshot changed", "Reload the snapshot before saving again.", "snapshot_changed"),
        _ => SnapshotProblem(409, "Action unavailable", "This snapshot is no longer eligible for this action.", "snapshot_not_reviewable")
    };

    /// <summary>Serves a private screenshot only to requests from the configured frontend.</summary>
    protected IActionResult Screenshot(ArchiveObject? stored)
    {
        if (stored is null) return NotFound();
        if (!HasTrustedFrontendRequest())
        {
            stored.Content.Dispose();
            return NotFound();
        }

        try
        {
            Response.Headers.XContentTypeOptions = "nosniff";
            return File(stored.Content, "image/png");
        }
        catch { stored.Content.Dispose(); throw; }
    }

    /// <summary>Serves private archived content within the same sandbox as public archived content.</summary>
    protected IActionResult Content(Guid snapshotId, string? path, SnapshotContentResult result,
        SnapshotPreviewScope scope, bool issuePreviewCookie)
    {
        if (result is SnapshotContentResult.NotFound) return NotFound();
        if (result is SnapshotContentResult.InvalidPath) return BadRequest();

        var stored = ((SnapshotContentResult.Found)result).Resource;
        try
        {
            if (!HasTrustedPreviewReferer(snapshotId, scope))
            {
                stored.Content.Dispose();
                return NotFound();
            }

            var apiOrigin = new Uri(options.PublicApiOrigin).GetLeftPart(UriPartial.Authority);
            Response.Headers.XContentTypeOptions = "nosniff";
            var isRootHtml = IsRootHtml(path, stored.ContentType);
            // Preserve the API origin so the snapshot-scoped cookie can authorize nested resource requests.
            Response.Headers.ContentSecurityPolicy =
                $"default-src 'none'; img-src {apiOrigin} data:; style-src {apiOrigin} 'unsafe-inline' data:; " +
                $"font-src {apiOrigin} data:; media-src {apiOrigin} data:; frame-src {apiOrigin}; " +
                "script-src 'none'; connect-src 'none'; object-src 'none'; form-action 'none'; " +
                $"base-uri {apiOrigin}; " +
                (stored.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true
                    ? $"frame-ancestors 'self' {options.FrontendOrigin.TrimEnd('/')}; " : "") +
                "sandbox allow-same-origin";
            Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            Response.Headers.CacheControl = "private, no-store";
            if (issuePreviewCookie && IsRootHtml(path, stored.ContentType))
                previewTickets.IssueCookie(Response, snapshotId, scope);
            if (stored.IsOriginalSource)
            {
                Response.Headers.ContentDisposition = "attachment";
                return File(stored.Content, "application/octet-stream", enableRangeProcessing: true);
            }
            return File(stored.Content, stored.ContentType ?? "application/octet-stream", enableRangeProcessing: true);
        }
        catch
        {
            stored.Content.Dispose();
            throw;
        }
    }

    private ObjectResult SnapshotProblem(int status, string title, string detail, string code) =>
        Problem(statusCode: status, title: title, detail: detail, extensions: new Dictionary<string, object?> { ["code"] = code });

    private static bool IsRootHtml(string? path, string? contentType) =>
        (string.IsNullOrWhiteSpace(path) || string.Equals(path.Trim('/'), "index.html", StringComparison.OrdinalIgnoreCase)) &&
        contentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>Allows private resources only when requested by the configured frontend or this snapshot's content.</summary>
    private bool HasTrustedPreviewReferer(Guid snapshotId, SnapshotPreviewScope scope)
    {
        var collection = scope == SnapshotPreviewScope.Owner ? "users" : "admin";
        var expectedPath = $"/api/{collection}/snapshots/{snapshotId:D}/content";
        var sameOriginFetch = string.Equals(Request.Headers["Sec-Fetch-Site"].ToString(), "same-origin",
            StringComparison.OrdinalIgnoreCase);
        var hasSnapshotTicket = previewTickets.IsValid(
            Request.Cookies[SnapshotPreviewTicketService.CookieName], snapshotId, scope);

        if (Uri.TryCreate(Request.Headers.Referer.ToString(), UriKind.Absolute, out var referer))
        {
            if (HasOrigin(referer, options.FrontendOrigin)) return true;
            if (!HasOrigin(referer, options.PublicApiOrigin)) return false;

            if (referer.AbsolutePath.Equals(expectedPath, StringComparison.OrdinalIgnoreCase) ||
                referer.AbsolutePath.StartsWith(expectedPath + "/", StringComparison.OrdinalIgnoreCase))
                return true;

            // A full API content path identifies a different snapshot and must never use the fallback.
            if (referer.AbsolutePath.StartsWith("/api/users/snapshots/", StringComparison.OrdinalIgnoreCase) ||
                referer.AbsolutePath.StartsWith("/api/admin/snapshots/", StringComparison.OrdinalIgnoreCase))
                return false;
        }

        // Archived pages can suppress Referer. Fetch Metadata alone is not snapshot-specific, so pair it
        // with the signed cookie whose payload and path are bound to the requested snapshot and scope.
        return sameOriginFetch && hasSnapshotTicket;
    }

    /// <summary>Allows private screenshot fetches from the configured application origin only.</summary>
    private bool HasTrustedFrontendRequest()
    {
        if (string.Equals(Request.Headers.Origin.ToString(), options.FrontendOrigin.TrimEnd('/'), StringComparison.Ordinal))
            return true;

        return Uri.TryCreate(Request.Headers.Referer.ToString(), UriKind.Absolute, out var referer) &&
               HasOrigin(referer, options.FrontendOrigin);
    }

    private static bool HasOrigin(Uri candidate, string configuredOrigin) =>
        Uri.TryCreate(configuredOrigin, UriKind.Absolute, out var configured) &&
        string.Equals(candidate.GetLeftPart(UriPartial.Authority), configured.GetLeftPart(UriPartial.Authority),
            StringComparison.OrdinalIgnoreCase);
}
