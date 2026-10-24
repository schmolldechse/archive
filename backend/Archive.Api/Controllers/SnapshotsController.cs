using Archive.Api.Authentication;
using Archive.Api.Contracts.Snapshots;
using Archive.Api.Contracts.Snapshots.Search;
using Archive.Api.Contracts.Snapshots.Submission;
using Archive.Api.Mapping;
using Archive.Api.Options;
using Archive.Api.Validation;
using Archive.Core.Models.Snapshots;
using Archive.Core.Repositories.SnapshotRepository;
using Archive.Core.Repositories.SnapshotProcessingRepository;
using Microsoft.AspNetCore.Mvc;

namespace Archive.Api.Controllers;

[ApiController]
[Route("api/snapshots")]
[Tags("Snapshots")]
public sealed partial class SnapshotsController(
    ISnapshotRepository snapshotRepository,
    SnapshotMapper mapper,
    SnapshotSubmissionMapper submissionMapper,
    ArchiveApiOptions options,
    ArchiveAuthenticationOptions authenticationOptions,
    ISnapshotProcessingRepository processing) : ControllerBase
{
    // Allow the configured viewer to embed archived documents without relaxing their sandbox.
    private string ArchivedContentPolicy(string origin, bool isRootHtml) =>
        $"default-src 'none'; img-src {origin} data:; style-src {origin} 'unsafe-inline' data:; " +
        $"font-src {origin} data:; media-src {origin} data:; frame-src {origin}; " +
        "script-src 'none'; connect-src 'none'; object-src 'none'; " +
        $"form-action 'none'; base-uri {origin}; " +
        (isRootHtml ? $"frame-ancestors 'self' {authenticationOptions.FrontendOrigin.TrimEnd('/')}; " : "") +
        "sandbox";

    private string ArchivedContentOrigin()
    {
        var publicBaseUrl = options.PublicBaseUrl;
        if (Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var publicUri) &&
            publicUri.Scheme is "http" or "https")
            return publicUri.GetLeftPart(UriPartial.Authority);

        return new UriBuilder(Request.Scheme, Request.Host.Host, Request.Host.Port ?? -1)
            .Uri.GetLeftPart(UriPartial.Authority);
    }

    [HttpGet]
    [EndpointName("ListSnapshots")]
    [EndpointSummary("Lists and searches approved archived snapshots.")]
    [EndpointDescription("Returns only approved snapshots with stored captures. Search results, annual capture counts and the global register summary share that visibility boundary. Annual counts precede capture-time filters.")]
    [ProducesResponseType<SnapshotListResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<SnapshotListResponse>> ListAsync(
        [FromQuery] SnapshotSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await snapshotRepository.SearchAsync(mapper.ToSearch(request), cancellationToken);
        if (result is SnapshotSearchResult.Invalid invalid)
        {
            ModelState.AddErrors(invalid.Errors);
            return ValidationProblem(ModelState);
        }

        var page = ((SnapshotSearchResult.Found)result).Page;
        var publicBaseUrl = (options.PublicBaseUrl ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        return Ok(mapper.ToResponse(page, publicBaseUrl));
    }

    [HttpGet("{snapshotId:guid}")]
    [EndpointName("GetSnapshot")]
    [EndpointSummary("Gets an approved snapshot.")]
    [EndpointDescription("Returns metadata and public resource URLs only for an approved stored snapshot. Other states return 404, including for signed-in moderators.")]
    [ProducesResponseType<SnapshotResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<SnapshotResponse>> GetAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        var snapshot = await snapshotRepository.GetByIdAsync(snapshotId, cancellationToken);
        if (snapshot is null)
            return NotFound();

        var publicBaseUrl = (options.PublicBaseUrl ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        return Ok(mapper.ToResponse(snapshot, publicBaseUrl));
    }

    [HttpGet("{snapshotId:guid}/history")]
    [EndpointName("GetSnapshotHistory")]
    [EndpointSummary("Gets public history for an approved snapshot.")]
    [EndpointDescription("Returns a fail-closed, sanitized page of public history. Moderation notes, rejection events, internal user identifiers and event actors are never returned.")]
    [ProducesResponseType<SnapshotHistoryResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<SnapshotHistoryResponse>> HistoryAsync(Guid snapshotId,
        [FromQuery] SnapshotSubmissionQuery query,
        CancellationToken cancellationToken)
    {
        var history = await snapshotRepository.HistoryAsync(snapshotId, query.Page, query.PageSize, cancellationToken);
        return history is null ? NotFound() : Ok(submissionMapper.ToResponse(history));
    }

    [HttpGet("{snapshotId:guid}/content/{**path}")]
    [EndpointName("GetSnapshotContent")]
    [EndpointSummary("Gets archived snapshot content.")]
    [EndpointDescription("Returns the archived start page when path is empty, or the requested stored resource. The response Content-Type depends on the resource. Original uploaded source files are downloaded as application/octet-stream. Byte Range requests are supported and may return HTTP 206 or 416. Content is served with a restrictive Content-Security-Policy.")]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "application/octet-stream", "text/html", "text/css", "image/png", "image/jpeg", "image/webp", "image/svg+xml")]
    [ProducesResponseType<Stream>(StatusCodes.Status206PartialContent, "application/octet-stream", "text/html", "text/css", "image/png", "image/jpeg", "image/webp", "image/svg+xml")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status416RangeNotSatisfiable)]
    public async Task<IActionResult> ContentAsync(Guid snapshotId, string? path, CancellationToken cancellationToken)
    {
        var result = await snapshotRepository.OpenContentAsync(snapshotId, path, cancellationToken);
        if (result is SnapshotContentResult.NotFound)
            return NotFound();
        if (result is SnapshotContentResult.InvalidPath)
            return StatusCode(StatusCodes.Status400BadRequest);
        var stored = ((SnapshotContentResult.Found)result).Resource;
        try
        {
            // URL captures are untrusted too, and content paths can be opened outside the preview iframe.
            Response.Headers.XContentTypeOptions = "nosniff";
            Response.Headers.AccessControlAllowOrigin = "*";
            Response.Headers.ContentSecurityPolicy = ArchivedContentPolicy(ArchivedContentOrigin(), IsRootHtml(path, stored.ContentType));
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
            await stored.Content.DisposeAsync();
            throw;
        }
    }

    private static bool IsRootHtml(string? path, string? contentType) =>
        (string.IsNullOrWhiteSpace(path) || string.Equals(path.Trim('/'), "index.html", StringComparison.OrdinalIgnoreCase)) &&
        contentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true;

    [HttpGet("{snapshotId:guid}/screenshot")]
    [EndpointName("GetSnapshotScreenshot")]
    [EndpointSummary("Gets a snapshot screenshot.")]
    [EndpointDescription("Returns the screenshot image created while archiving the snapshot.")]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "image/png")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> ScreenshotAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        var stored = await snapshotRepository.OpenScreenshotAsync(snapshotId, cancellationToken);
        if (stored is null)
            return NotFound();
        try
        {
            Response.Headers.XContentTypeOptions = "nosniff";
            return File(stored.Content, "image/png");
        }
        catch
        {
            await stored.Content.DisposeAsync();
            throw;
        }
    }

}
