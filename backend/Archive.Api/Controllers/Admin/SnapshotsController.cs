using Archive.Api.Authentication;
using Archive.Api.Authorization.Attributes;
using Archive.Api.Contracts.Snapshots.Submission;
using Archive.Api.Mapping;
using Archive.Core.Authorization;
using Archive.Core.Models.Snapshots;
using Archive.Core.Repositories.SnapshotSubmissionRepository;
using Archive.Core.Repositories.UserRepository;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Archive.Api.Controllers.Admin;

/// <summary>Private snapshot administration, review queue and conditional moderation decisions.</summary>
[ApiController, Route("api/admin/snapshots"), Tags("Snapshot Moderation")]
[RequirePermission(ArchivePermissions.AccessAdminPortal), RequirePermission(ArchivePermissions.AccessAdminSnapshots)]
[EnableCors(ArchiveAuthenticationExtension.CorsPolicy)]
[ProducesResponseType<ProblemDetails>(401), ProducesResponseType<ProblemDetails>(403)]
public sealed class SnapshotsController(ISnapshotSubmissionRepository submissions, SnapshotSubmissionMapper mapper,
    ArchiveAuthenticationOptions options, IUserRepository users) : SnapshotSubmissionControllerBase(submissions, mapper, options)
{
    [HttpGet, EndpointName("ListAdminSnapshots"), EndpointSummary("Lists snapshot records for administration.")]
    [ProducesResponseType<SnapshotSubmissionListResponse>(200), ProducesResponseType<ValidationProblemDetails>(400)]
    [EndpointDescription("Returns a bounded page of snapshot records across all moderation and capture states. Supports title or source URL search, exact moderation status, and complete or not-fully-captured filters.")]
    public async Task<ActionResult<SnapshotSubmissionListResponse>> List([FromQuery] SnapshotAdministrationQuery query, CancellationToken token) =>
        Ok(Mapper.ToResponse(await Submissions.SearchForAdminAsync(query.Q, query.ModerationStatus, query.Quality, query.Page, query.PageSize, token)));

    [HttpGet("{id:guid}"), EndpointName("GetSnapshotForReview"), EndpointSummary("Reads private snapshot details and a strong ETag.")]
    [ProducesResponseType<SnapshotSubmissionDetailResponse>(200), ProducesResponseType<ProblemDetails>(404)]
    public async Task<ActionResult<SnapshotSubmissionDetailResponse>> Get(Guid id, CancellationToken token) =>
        Detail(await Submissions.GetForReviewAsync(id, token), "api/admin/snapshots");

    [HttpGet("owners"), EndpointName("ListSnapshotOwners"), EndpointSummary("Lists registered snapshot owner candidates.")]
    [EndpointDescription("Requires snapshot administration access. Returns public user contracts only; internal user identifiers are never exposed.")]
    [ProducesResponseType<SnapshotOwnerListResponse>(200), ProducesResponseType<ValidationProblemDetails>(400)]
    public async Task<ActionResult<SnapshotOwnerListResponse>> Owners([FromQuery] SnapshotOwnerQuery query,
        CancellationToken token) =>
        Ok(Mapper.ToOwnerResponse(await users.SearchAsync(query.Q, null, query.Page, query.PageSize, token)));

    [HttpPut("{id:guid}"), Consumes("application/json"), EndpointName("UpdateSnapshotForAdmin"),
     EndpointSummary("Updates a snapshot and its optional account attribution.")]
    [EndpointDescription("Requires snapshot administration access, the configured frontend Origin and exactly one strong If-Match from GET. Administrative edits never change moderation status. A null createdByHandle removes account attribution.")]
    [ProducesResponseType<SnapshotSubmissionDetailResponse>(200), ProducesResponseType<ValidationProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401), ProducesResponseType<ProblemDetails>(403),
     ProducesResponseType<ProblemDetails>(404), ProducesResponseType<ProblemDetails>(409),
     ProducesResponseType<ProblemDetails>(412), ProducesResponseType<ProblemDetails>(428)]
    public async Task<ActionResult<SnapshotSubmissionDetailResponse>> Update(Guid id,
        [FromBody] UpdateSnapshotForAdminRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken token)
    {
        if (ValidateWrite(out var tag) is { } error) return error;
        var outcome = await Submissions.UpdateForAdminAsync(User.ReadUserId(), id, tag, request.ToModel(), token);
        if (UpdateOutcome(outcome) is { } failure) return failure;
        return Detail(await Submissions.GetForReviewAsync(id, token), "api/admin/snapshots");
    }

    [HttpGet("{id:guid}/history"), EndpointName("GetSnapshotHistoryForReview"), EndpointSummary("Reads general snapshot lifecycle history.")]
    [ProducesResponseType<SnapshotHistoryResponse>(200), ProducesResponseType<ProblemDetails>(404), ProducesResponseType<ValidationProblemDetails>(400)]
    public async Task<ActionResult<SnapshotHistoryResponse>> History(Guid id, [FromQuery] SnapshotSubmissionQuery query, CancellationToken token)
    {
        var history = await Submissions.HistoryForReviewAsync(id, query.Page, query.PageSize, token);
        return history is null ? NotFound() : Ok(Mapper.ToResponse(history));
    }

    [HttpGet("{id:guid}/screenshot"), EndpointName("GetSnapshotScreenshotForReview"), EndpointSummary("Reads a private capture preview.")]
    [ProducesResponseType<Stream>(200, "image/png"), ProducesResponseType<ProblemDetails>(404)]
    public async Task<IActionResult> Preview(Guid id, CancellationToken token) => Screenshot(await Submissions.ScreenshotForReviewAsync(id, token));

    [HttpGet("{id:guid}/content/{**path}"), EndpointName("GetSnapshotContentForReview"), EndpointSummary("Reads private archived content for review.")]
    [ProducesResponseType<Stream>(200, "application/octet-stream"), ProducesResponseType<Stream>(206, "application/octet-stream")]
    [ProducesResponseType<ProblemDetails>(400), ProducesResponseType<ProblemDetails>(404)]
    public async Task<IActionResult> Content(Guid id, string? path, CancellationToken token) =>
        base.Content(
            id,
            "api/admin/snapshots",
            await Submissions.OpenContentForReviewAsync(id, path, token));

    [HttpPost("{id:guid}/review"), Consumes("application/json"), EndpointName("ReviewSnapshot"), EndpointSummary("Approves or rejects a captured snapshot with an optional reason.")]
    [EndpointDescription("Requires admin.access and admin.snapshots.access, the configured frontend Origin, and exactly one strong If-Match from GET on this resource.")]
    [ProducesResponseType<SnapshotSubmissionDetailResponse>(200), ProducesResponseType<ValidationProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404), ProducesResponseType<ProblemDetails>(409), ProducesResponseType<ProblemDetails>(412), ProducesResponseType<ProblemDetails>(428)]
    public async Task<ActionResult<SnapshotSubmissionDetailResponse>> Review(Guid id, [FromBody] ReviewSnapshotRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken token)
    {
        if (ValidateWrite(out var tag) is { } error) return error;
        var outcome = await Submissions.ReviewAsync(User.ReadUserId(), id, tag, request.Decision, request.Reason, token);
        return outcome == SnapshotReviewOutcome.Updated
            ? Detail(await Submissions.GetForReviewAsync(id, token), "api/admin/snapshots")
            : Outcome(outcome);
    }
}
