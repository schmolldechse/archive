using Archive.Api.Authentication;
using Archive.Api.Contracts.Snapshots.Submission;
using Archive.Api.Mapping;
using Archive.Api.Validation;
using Archive.Core.Models.Snapshots;
using Archive.Core.Repositories.SnapshotSubmissionRepository;
using Archive.Core.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Archive.Api.Controllers;

/// <summary>Shared private response and conditional-write handling for owners and moderators.</summary>
public abstract class SnapshotSubmissionControllerBase(ISnapshotSubmissionRepository submissions,
    SnapshotSubmissionMapper mapper, ArchiveAuthenticationOptions options) : ControllerBase
{
    protected ISnapshotSubmissionRepository Submissions => submissions;
    protected SnapshotSubmissionMapper Mapper => mapper;

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

    /// <summary>Maps update failures while leaving successful outcomes to the scope-specific detail reload.</summary>
    protected ActionResult<SnapshotSubmissionDetailResponse>? UpdateOutcome(SnapshotUpdateResult outcome)
    {
        switch (outcome)
        {
            case SnapshotUpdateResult.Updated:
            case SnapshotUpdateResult.Unchanged:
                return null;
            case SnapshotUpdateResult.Invalid invalid:
                ModelState.AddErrors(invalid.Errors);
                return ValidationProblem(ModelState);
            case SnapshotUpdateResult.NotFound:
                return NotFound();
            case SnapshotUpdateResult.Forbidden:
                return Forbid();
            case SnapshotUpdateResult.Stale:
                return SnapshotProblem(412, "Snapshot changed", "Reload the snapshot before saving again.", "snapshot_changed");
            case SnapshotUpdateResult.SourceNotEditable:
                return SnapshotProblem(409, "Source URL cannot be changed",
                    "Wait until this snapshot has a stored capture before changing its source URL.",
                    "snapshot_source_not_editable");
            default:
                throw new InvalidOperationException("Unknown snapshot update result.");
        }
    }

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

    /// <summary>Serves controller-authorized private content in a sandbox scoped to this snapshot's resources.</summary>
    protected IActionResult Content(Guid snapshotId, string routeBase, SnapshotContentResult result)
    {
        if (result is SnapshotContentResult.NotFound) return NotFound();
        if (result is SnapshotContentResult.InvalidPath) return BadRequest();

        var stored = ((SnapshotContentResult.Found)result).Resource;
        try
        {
            if (RequiresTrustedPreviewNavigationSource(stored.ContentType) &&
                !HasTrustedPreviewNavigationSource(snapshotId, routeBase))
            {
                stored.Content.Dispose();
                return NotFound();
            }

            var apiOrigin = new Uri(options.PublicApiOrigin).GetLeftPart(UriPartial.Authority);
            var contentBaseUrl = $"{apiOrigin}/{routeBase.Trim('/')}/{snapshotId:D}/content/";
            Response.Headers.XContentTypeOptions = "nosniff";
            Response.Headers.ContentSecurityPolicy =
                $"default-src 'none'; img-src {contentBaseUrl} data:; style-src {contentBaseUrl} 'unsafe-inline' data:; " +
                $"font-src {contentBaseUrl} data:; media-src {contentBaseUrl} data:; frame-src {contentBaseUrl}; " +
                "script-src 'none'; connect-src 'none'; object-src 'none'; form-action 'none'; " +
                $"base-uri {contentBaseUrl}; " +
                (stored.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true
                    ? $"frame-ancestors 'self' {options.FrontendOrigin.TrimEnd('/')}; " : "") +
                "sandbox allow-same-origin";
            // Full same-origin referrers bind later frame navigations to the current snapshot.
            Response.Headers["Referrer-Policy"] = "same-origin";
            Response.Headers.CacheControl = "private, no-store";
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

    protected ObjectResult SnapshotProblem(int status, string title, string detail, string code) =>
        Problem(statusCode: status, title: title, detail: detail, extensions: new Dictionary<string, object?> { ["code"] = code });

    /// <summary>Recognizes browser navigations and fails closed for HTML when fetch metadata is unavailable.</summary>
    private bool RequiresTrustedPreviewNavigationSource(string? contentType) =>
        string.Equals(Request.Headers["Sec-Fetch-Mode"].ToString(), "navigate", StringComparison.OrdinalIgnoreCase) ||
        contentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>Allows the initial frontend frame and later navigations originating within the same snapshot.</summary>
    private bool HasTrustedPreviewNavigationSource(Guid snapshotId, string routeBase)
    {
        if (!Uri.TryCreate(Request.Headers.Referer.ToString(), UriKind.Absolute, out var referer))
            return false;
        if (HasOrigin(referer, options.FrontendOrigin))
            return true;
        if (!HasOrigin(referer, options.PublicApiOrigin))
            return false;

        var expectedPath = $"/{routeBase.Trim('/')}/{snapshotId:D}/content";
        return referer.AbsolutePath.Equals(expectedPath, StringComparison.OrdinalIgnoreCase) ||
               referer.AbsolutePath.StartsWith(expectedPath + "/", StringComparison.OrdinalIgnoreCase);
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
