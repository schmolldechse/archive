using Archive.Api.Authentication;
using Archive.Api.Contracts.Snapshots.Submission;
using Archive.Api.Mapping;
using Archive.Api.Snapshots;
using Archive.Core.Models.Snapshots;
using Archive.Core.Repositories.SnapshotSubmissionRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Archive.Api.Controllers.User;

/// <summary>Current-account submissions; IDs belonging to another account are always missing.</summary>
[ApiController, Route("api/users/snapshots"), Tags("My Submissions"), Authorize]
[EnableCors(ArchiveAuthenticationExtension.CorsPolicy)]
[ProducesResponseType<ProblemDetails>(401)]
public sealed class SnapshotsController(ISnapshotSubmissionRepository submissions, SnapshotSubmissionMapper mapper,
    ArchiveAuthenticationOptions options, SnapshotPreviewTicketService previewTickets)
    : SnapshotSubmissionControllerBase(submissions, mapper, options, previewTickets)
{
    [HttpGet, EndpointName("ListOwnSnapshots"), EndpointSummary("Lists the current account's submissions and their statuses.")]
    [ServiceFilter(typeof(ExistingArchiveUserFilter))]
    [ProducesResponseType<SnapshotSubmissionListResponse>(200), ProducesResponseType<ValidationProblemDetails>(400)]
    public async Task<ActionResult<SnapshotSubmissionListResponse>> List([FromQuery] SnapshotSubmissionQuery query, CancellationToken token) =>
        Ok(Mapper.ToResponse(await Submissions.SearchOwnAsync(User.ReadUserId(), query.Q, query.ModerationStatus, query.Page, query.PageSize, token)));

    [HttpGet("{id:guid}"), EndpointName("GetOwnSnapshot"), EndpointSummary("Reads an own submission and a strong ETag.")]
    [ServiceFilter(typeof(ExistingArchiveUserFilter))]
    [ProducesResponseType<SnapshotSubmissionDetailResponse>(200), ProducesResponseType<ProblemDetails>(404)]
    public async Task<ActionResult<SnapshotSubmissionDetailResponse>> Get(Guid id, CancellationToken token) =>
        Detail(await Submissions.GetOwnAsync(User.ReadUserId(), id, token), "api/users/snapshots");

    [HttpGet("{id:guid}/history"), EndpointName("GetOwnSnapshotHistory"), EndpointSummary("Reads permitted lifecycle events for an own snapshot.")]
    [ServiceFilter(typeof(ExistingArchiveUserFilter))]
    [ProducesResponseType<SnapshotHistoryResponse>(200), ProducesResponseType<ProblemDetails>(404), ProducesResponseType<ValidationProblemDetails>(400)]
    public async Task<ActionResult<SnapshotHistoryResponse>> History(Guid id, [FromQuery] SnapshotSubmissionQuery query, CancellationToken token)
    {
        var history = await Submissions.HistoryOwnAsync(User.ReadUserId(), id, query.Page, query.PageSize, token);
        return history is null ? NotFound() : Ok(Mapper.ToResponse(history));
    }

    [HttpGet("{id:guid}/screenshot"), EndpointName("GetOwnSnapshotScreenshot"), EndpointSummary("Reads an own private capture preview.")]
    [ServiceFilter(typeof(ExistingArchiveUserFilter))]
    [ProducesResponseType<Stream>(200, "image/png"), ProducesResponseType<ProblemDetails>(404)]
    public async Task<IActionResult> Preview(Guid id, CancellationToken token) => Screenshot(await Submissions.ScreenshotOwnAsync(User.ReadUserId(), id, token));

    [HttpGet("{id:guid}/content/{**path}"), EndpointName("GetOwnSnapshotContent"), EndpointSummary("Reads own private archived content.")]
    [AllowAnonymous]
    [ProducesResponseType<Stream>(200, "application/octet-stream"), ProducesResponseType<Stream>(206, "application/octet-stream")]
    [ProducesResponseType<ProblemDetails>(400), ProducesResponseType<ProblemDetails>(404)]
    public async Task<IActionResult> Content(Guid id, string? path, CancellationToken token)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var ownContent = await Submissions.OpenContentOwnAsync(User.ReadUserId(), id, path, token);
            return base.Content(id, path, ownContent, SnapshotPreviewScope.Owner, issuePreviewCookie: true);
        }

        if (!PreviewTickets.IsValid(Request.Cookies[SnapshotPreviewTicketService.CookieName], id,
                SnapshotPreviewScope.Owner))
            return Unauthorized();

        var previewContent = await Submissions.OpenContentForReviewAsync(id, path, token);
        return base.Content(id, path, previewContent, SnapshotPreviewScope.Owner, issuePreviewCookie: false);
    }

    [HttpPost("{id:guid}/resubmit"), EndpointName("ResubmitSnapshot"), EndpointSummary("Requests another review without changing the snapshot.")]
    [ServiceFilter(typeof(ExistingArchiveUserFilter))]
    [EndpointDescription("Requires the owning account, the configured frontend Origin and exactly one strong If-Match from GET on the own submission resource. No body is accepted or needed.")]
    [ProducesResponseType<SnapshotSubmissionDetailResponse>(200), ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(403), ProducesResponseType<ProblemDetails>(404), ProducesResponseType<ProblemDetails>(409), ProducesResponseType<ProblemDetails>(412), ProducesResponseType<ProblemDetails>(428)]
    public async Task<ActionResult<SnapshotSubmissionDetailResponse>> Resubmit(Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken token)
    {
        if (ValidateWrite(out var tag) is { } error) return error;
        if (Request.ContentLength is > 0 || Request.Headers.TransferEncoding.Count > 0)
            return Problem(statusCode: 400, title: "Request body not supported",
                detail: "Request another review without submitting changes to this snapshot.");
        var outcome = await Submissions.ResubmitAsync(User.ReadUserId(), id, tag, token);
        return outcome == SnapshotReviewOutcome.Updated
            ? Detail(await Submissions.GetOwnAsync(User.ReadUserId(), id, token), "api/users/snapshots")
            : Outcome(outcome);
    }
}
