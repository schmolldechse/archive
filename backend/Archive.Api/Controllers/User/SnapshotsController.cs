using Archive.Api.Authentication;
using Archive.Api.Authorization;
using Archive.Api.Contracts.Snapshots.Submission;
using Archive.Api.Mapping;
using Archive.Core.Models.Snapshots;
using Archive.Core.Repositories.SnapshotSubmissionRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Archive.Api.Controllers.User;

/// <summary>Current-account submissions; IDs belonging to another account are always missing.</summary>
[ApiController, Route("api/users/snapshots"), Tags("My Submissions"),
 Authorize(Policy = ArchiveAuthorizationPolicies.RegisteredUser)]
[EnableCors(ArchiveAuthenticationExtension.CorsPolicy)]
[ProducesResponseType<ProblemDetails>(401)]
public sealed class SnapshotsController(ISnapshotSubmissionRepository submissions, SnapshotSubmissionMapper mapper,
    ArchiveAuthenticationOptions options) : SnapshotSubmissionControllerBase(submissions, mapper, options)
{
    [HttpGet, EndpointName("ListOwnSnapshots"), EndpointSummary("Lists the current account's submissions and their statuses.")]
    [ProducesResponseType<SnapshotSubmissionListResponse>(200), ProducesResponseType<ValidationProblemDetails>(400)]
    public async Task<ActionResult<SnapshotSubmissionListResponse>> List([FromQuery] SnapshotSubmissionQuery query, CancellationToken token) =>
        Ok(Mapper.ToResponse(await Submissions.SearchOwnAsync(User.ReadUserId(), query.Q, query.ModerationStatus, query.Page, query.PageSize, token)));

    [HttpGet("{id:guid}"), EndpointName("GetOwnSnapshot"), EndpointSummary("Reads an own submission and a strong ETag.")]
    [ProducesResponseType<SnapshotSubmissionDetailResponse>(200), ProducesResponseType<ProblemDetails>(404)]
    public async Task<ActionResult<SnapshotSubmissionDetailResponse>> Get(Guid id, CancellationToken token) =>
        Detail(await Submissions.GetOwnAsync(User.ReadUserId(), id, token), "api/users/snapshots");

    [HttpPut("{id:guid}"), Consumes("application/json"), EndpointName("UpdateOwnSnapshot"),
     EndpointSummary("Updates editable fields of an own snapshot.")]
    [EndpointDescription("Requires the owning account, the configured frontend Origin and exactly one strong If-Match from GET. A changed source URL moves a stored snapshot to RESUBMITTED; title, description and tag changes retain moderation status.")]
    [ProducesResponseType<SnapshotSubmissionDetailResponse>(200), ProducesResponseType<ValidationProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401), ProducesResponseType<ProblemDetails>(403),
     ProducesResponseType<ProblemDetails>(404), ProducesResponseType<ProblemDetails>(409),
     ProducesResponseType<ProblemDetails>(412), ProducesResponseType<ProblemDetails>(428)]
    public async Task<ActionResult<SnapshotSubmissionDetailResponse>> Update(Guid id,
        [FromBody] UpdateOwnSnapshotRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken token)
    {
        if (ValidateWrite(out var tag) is { } error) return error;
        var outcome = await Submissions.UpdateOwnAsync(User.ReadUserId(), id, tag, request.ToModel(), token);
        if (UpdateOutcome(outcome) is { } failure) return failure;
        return Detail(await Submissions.GetOwnAsync(User.ReadUserId(), id, token), "api/users/snapshots");
    }

    [HttpGet("{id:guid}/history"), EndpointName("GetOwnSnapshotHistory"), EndpointSummary("Reads permitted lifecycle events for an own snapshot.")]
    [ProducesResponseType<SnapshotHistoryResponse>(200), ProducesResponseType<ProblemDetails>(404), ProducesResponseType<ValidationProblemDetails>(400)]
    public async Task<ActionResult<SnapshotHistoryResponse>> History(Guid id, [FromQuery] SnapshotSubmissionQuery query, CancellationToken token)
    {
        var history = await Submissions.HistoryOwnAsync(User.ReadUserId(), id, query.Page, query.PageSize, token);
        return history is null ? NotFound() : Ok(Mapper.ToResponse(history));
    }

    [HttpGet("{id:guid}/screenshot"), EndpointName("GetOwnSnapshotScreenshot"), EndpointSummary("Reads an own private capture preview.")]
    [ProducesResponseType<Stream>(200, "image/png"), ProducesResponseType<ProblemDetails>(404)]
    public async Task<IActionResult> Preview(Guid id, CancellationToken token) => Screenshot(await Submissions.ScreenshotOwnAsync(User.ReadUserId(), id, token));

    [HttpPost("{id:guid}/resubmit"), EndpointName("ResubmitSnapshot"), EndpointSummary("Requests another review without changing the snapshot.")]
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
