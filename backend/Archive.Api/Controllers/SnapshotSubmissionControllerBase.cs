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
        return Ok(mapper.ToResponse(detail, resourceBaseUrl, User.ReadUserId()));
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

    /// <summary>Maps a completed conditional deletion without exposing owner-scope misses.</summary>
    protected IActionResult DeletionOutcome(SnapshotDeletionResult outcome) => outcome switch
    {
        SnapshotDeletionResult.Deleted => NoContent(),
        SnapshotDeletionResult.NotFound => NotFound(),
        SnapshotDeletionResult.Forbidden => Forbid(),
        SnapshotDeletionResult.Stale => SnapshotProblem(412, "Snapshot changed",
            "Reload the snapshot before deleting it.", "snapshot_changed"),
        _ => throw new InvalidOperationException("Unknown snapshot deletion result.")
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

    protected ObjectResult SnapshotProblem(int status, string title, string detail, string code) =>
        Problem(statusCode: status, title: title, detail: detail, extensions: new Dictionary<string, object?> { ["code"] = code });

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
