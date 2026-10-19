using Archive.Api.Authentication;
using Archive.Api.Contracts.Snapshots.Submission;
using Archive.Api.Mapping;
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

    protected IActionResult Screenshot(ArchiveObject? stored)
    {
        if (stored is null) return NotFound();
        try
        {
            Response.Headers.XContentTypeOptions = "nosniff";
            return File(stored.Content, "image/png");
        }
        catch { stored.Content.Dispose(); throw; }
    }

    /// <summary>Serves private archived content within the same sandbox as public archived content.</summary>
    protected IActionResult Content(SnapshotContentResult result)
    {
        if (result is SnapshotContentResult.NotFound) return NotFound();
        if (result is SnapshotContentResult.InvalidPath) return BadRequest();

        var stored = ((SnapshotContentResult.Found)result).Resource;
        try
        {
            var apiOrigin = new Uri(options.PublicApiOrigin).GetLeftPart(UriPartial.Authority);
            Response.Headers.XContentTypeOptions = "nosniff";
            Response.Headers.ContentSecurityPolicy =
                $"default-src 'none'; img-src {apiOrigin} data:; style-src {apiOrigin} 'unsafe-inline' data:; " +
                $"font-src {apiOrigin} data:; media-src {apiOrigin} data:; frame-src {apiOrigin}; " +
                "script-src 'none'; connect-src 'none'; object-src 'none'; form-action 'none'; " +
                $"base-uri {apiOrigin}; frame-ancestors 'self' {options.FrontendOrigin.TrimEnd('/')}; sandbox";
            Response.Headers["Referrer-Policy"] = "no-referrer";
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
}
