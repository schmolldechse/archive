using Archive.Api.Contracts;
using Archive.Api.Mapping;
using Archive.Api.Options;
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
    ArchiveApiOptions options,
    ISnapshotProcessingRepository processing) : ControllerBase
{
    private static string ArchivedContentPolicy(string origin) =>
        $"default-src 'none'; img-src {origin} data:; style-src {origin} 'unsafe-inline' data:; " +
        $"font-src {origin} data:; media-src {origin} data:; frame-src {origin}; " +
        "script-src 'none'; connect-src 'none'; object-src 'none'; " +
        $"form-action 'none'; base-uri {origin}; frame-ancestors 'self'; sandbox";

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
    [EndpointSummary("Lists and searches archived snapshots.")]
    [EndpointDescription("Returns a stably ordered page of snapshots using explicit text, source, tag, quality, source-type, and capture-time filters, with annual capture counts before capture-time filters and a global register summary.")]
    [ProducesResponseType<SnapshotListResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<SnapshotListResponse>> ListAsync(
        [FromQuery] SnapshotSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (Request.Query.TryGetValue("order", out var orderValues) &&
            (orderValues.Count != 1 || orderValues[0] is not ("asc" or "desc")))
        {
            ModelState.AddModelError(nameof(request.Order), "Capture order must be asc or desc.");
            return ValidationProblem(ModelState);
        }

        var result = await snapshotRepository.SearchAsync(mapper.ToSearch(request), cancellationToken);
        if (result is SnapshotSearchResult.Invalid invalid)
        {
            foreach (var error in invalid.Errors)
                ModelState.AddModelError(error.Field, error.Message);
            return ValidationProblem(ModelState);
        }

        var page = ((SnapshotSearchResult.Found)result).Page;
        var publicBaseUrl = (options.PublicBaseUrl ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        return Ok(mapper.ToResponse(page, publicBaseUrl));
    }

    [HttpGet("{snapshotId:guid}")]
    [EndpointName("GetSnapshot")]
    [EndpointSummary("Gets a snapshot.")]
    [EndpointDescription("Returns metadata and public resource URLs for a stored snapshot.")]
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
            Response.Headers.ContentSecurityPolicy = ArchivedContentPolicy(ArchivedContentOrigin());
            Response.Headers["Referrer-Policy"] = "no-referrer";
            if (stored.IsOriginalSource)
            {
                Response.Headers.ContentDisposition = "attachment";
                return File(stored.Content, "application/octet-stream", enableRangeProcessing: true);
            }
            return File(stored.Content, stored.ContentType, enableRangeProcessing: true);
        }
        catch
        {
            await stored.Content.DisposeAsync();
            throw;
        }
    }

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
