using Archive.Api.Contracts;
using Archive.Core;
using Archive.Core.Entities;
using Archive.Core.Jobs;
using Archive.Core.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Archive.Api.Controllers;

[ApiController]
[Route("api/snapshots")]
[Tags("Snapshots")]
public sealed class SnapshotsController(
    DataContext database,
    IArchiveObjectStore objectStore,
    IConfiguration configuration) : ControllerBase
{
    private static string ArchivedContentPolicy(string origin) =>
        $"default-src 'none'; img-src {origin} data:; style-src {origin} 'unsafe-inline' data:; " +
        $"font-src {origin} data:; media-src {origin} data:; frame-src {origin}; " +
        "script-src 'none'; connect-src 'none'; object-src 'none'; " +
        $"form-action 'none'; base-uri {origin}; frame-ancestors 'self'; sandbox";

    private string ArchivedContentOrigin()
    {
        var publicBaseUrl = configuration["PublicBaseUrl"];
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
    [ProducesResponseType<SnapshotListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
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

        if (request.CapturedFrom is not null && request.CapturedUntil is not null && request.CapturedFrom >= request.CapturedUntil)
        {
            ModelState.AddModelError(nameof(request.CapturedUntil), "The end of the capture range must be later than its start.");
            return ValidationProblem(ModelState);
        }

        string? normalizedSourceUrl = null;
        if (!string.IsNullOrWhiteSpace(request.SourceUrl) &&
            !UrlNormalizer.TryNormalizeSource(request.SourceUrl, out normalizedSourceUrl, out var sourceUrlError))
        {
            ModelState.AddModelError(nameof(request.SourceUrl), sourceUrlError);
            return ValidationProblem(ModelState);
        }

        var textTerms = SplitTerms(request.Text);
        if (textTerms.Length > 20)
        {
            ModelState.AddModelError(nameof(request.Text), "The text search accepts at most 20 terms.");
            return ValidationProblem(ModelState);
        }

        var requestedTags = NormalizeTags(request.Tags);
        if (requestedTags is null)
            return ValidationProblem(ModelState);

        var query = database.Snapshots.AsNoTracking().AsQueryable();

        foreach (var term in textTerms)
        {
            var pattern = ContainsPattern(term);
            query = query.Where(snapshot =>
                EF.Functions.ILike(snapshot.Title, pattern, "\\") ||
                (snapshot.Description != null && EF.Functions.ILike(snapshot.Description, pattern, "\\")) ||
                EF.Functions.ILike(snapshot.OriginUrl ?? string.Empty, pattern, "\\") ||
                snapshot.SnapshotTags.Any(snapshotTag => EF.Functions.ILike(snapshotTag.Tag.Value, pattern, "\\")));
        }

        if (normalizedSourceUrl is not null)
            query = query.Where(snapshot => snapshot.OriginUrl == normalizedSourceUrl);

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            var pattern = ContainsPattern(request.Title);
            query = query.Where(snapshot => EF.Functions.ILike(snapshot.Title, pattern, "\\"));
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            var pattern = ContainsPattern(request.Description);
            query = query.Where(snapshot =>
                snapshot.Description != null && EF.Functions.ILike(snapshot.Description, pattern, "\\"));
        }

        foreach (var tag in requestedTags)
        {
            var pattern = ExactPattern(tag);
            query = query.Where(snapshot =>
                snapshot.SnapshotTags.Any(snapshotTag => EF.Functions.ILike(snapshotTag.Tag.Value, pattern, "\\")));
        }

        if (request.SourceType is not null)
            query = query.Where(snapshot => snapshot.SourceType == request.SourceType);

        if (request.Quality is not null)
            query = query.Where(snapshot => snapshot.Quality == request.Quality);

        var years = await query
            .GroupBy(snapshot => snapshot.CreatedAt.Year)
            .OrderBy(group => group.Key)
            .Select(group => new CaptureYearResponse(group.Key, group.Count()))
            .ToArrayAsync(cancellationToken);
        var distribution = new CaptureDistributionResponse(
            years.Sum(year => year.Count),
            years.Length == 0 ? null : years[0].Year,
            years.Length == 0 ? null : years[^1].Year,
            years);
        var indexTotal = await database.Snapshots.CountAsync(cancellationToken);
        var recentCaptureTimes = await database.Snapshots.AsNoTracking()
            .OrderByDescending(snapshot => snapshot.CreatedAt)
            .ThenByDescending(snapshot => snapshot.Id)
            .Select(snapshot => snapshot.CreatedAt)
            .Take(4)
            .ToArrayAsync(cancellationToken);

        if (request.CapturedFrom is not null)
        {
            var capturedFrom = request.CapturedFrom.Value.ToUniversalTime();
            query = query.Where(snapshot => snapshot.CreatedAt >= capturedFrom);
        }

        if (request.CapturedUntil is not null)
        {
            var capturedUntil = request.CapturedUntil.Value.ToUniversalTime();
            query = query.Where(snapshot => snapshot.CreatedAt < capturedUntil);
        }

        var total = await query.CountAsync(cancellationToken);
        var offset = (request.Page - 1) * request.PageSize;
        var orderedQuery = request.Order == SnapshotOrder.Asc
            ? query.OrderBy(snapshot => snapshot.CreatedAt).ThenBy(snapshot => snapshot.Id)
            : query.OrderByDescending(snapshot => snapshot.CreatedAt).ThenByDescending(snapshot => snapshot.Id);
        var page = await orderedQuery
            .Skip(offset)
            .Take(request.PageSize)
            .Include(snapshot => snapshot.SnapshotTags)
            .ThenInclude(snapshotTag => snapshotTag.Tag)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var publicBaseUrl = (configuration["PublicBaseUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        return Ok(new SnapshotListResponse(
            page.Select(snapshot => snapshot.ToResponse(publicBaseUrl)).ToArray(),
            total,
            request.Page,
            request.PageSize,
            indexTotal,
            recentCaptureTimes,
            distribution));
    }

    [HttpGet("{snapshotId:guid}")]
    [EndpointName("GetSnapshot")]
    [EndpointSummary("Gets a snapshot.")]
    [EndpointDescription("Returns metadata and public resource URLs for a stored snapshot.")]
    [ProducesResponseType<SnapshotResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SnapshotResponse>> GetAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        var snapshot = await database.Snapshots.AsNoTracking().Include(x => x.SnapshotTags).ThenInclude(x => x.Tag).SingleOrDefaultAsync(x => x.Id == snapshotId, cancellationToken);
        if (snapshot is null)
            return NotFound();

        var publicBaseUrl = (configuration["PublicBaseUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        return Ok(snapshot.ToResponse(publicBaseUrl));
    }

    [HttpGet("{snapshotId:guid}/content/{**path}")]
    [EndpointName("GetSnapshotContent")]
    [EndpointSummary("Gets archived snapshot content.")]
    [EndpointDescription("Returns a stored content resource; without a path, the archived start page is returned.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ContentAsync(Guid snapshotId, string? path, CancellationToken cancellationToken)
    {
        var snapshot = await database.Snapshots.AsNoTracking().SingleOrDefaultAsync(x => x.Id == snapshotId, cancellationToken);
        if (snapshot is null)
            return NotFound();

        var safePath = string.IsNullOrWhiteSpace(path) ? "index.html" : path.Replace('\\', '/').TrimStart('/');
        if (safePath.Contains("..", StringComparison.Ordinal))
            return StatusCode(StatusCodes.Status400BadRequest);

        var stored = await objectStore.GetAsync($"{snapshot.ContentPrefix}/{safePath}", cancellationToken);
        if (stored is null)
            return NotFound();
        // URL captures are untrusted too, and content paths can be opened outside the preview iframe.
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.ContentSecurityPolicy = ArchivedContentPolicy(ArchivedContentOrigin());
        Response.Headers["Referrer-Policy"] = "no-referrer";
        if (snapshot.SourceType != SourceType.Url && safePath.StartsWith("source.", StringComparison.OrdinalIgnoreCase))
        {
            Response.Headers.ContentDisposition = "attachment";
            return File(stored.Content, "application/octet-stream", enableRangeProcessing: true);
        }
        return File(stored.Content, stored.ContentType, enableRangeProcessing: true);
    }

    [HttpGet("{snapshotId:guid}/screenshot")]
    [EndpointName("GetSnapshotScreenshot")]
    [EndpointSummary("Gets a snapshot screenshot.")]
    [EndpointDescription("Returns the screenshot image created while archiving the snapshot.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ScreenshotAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        var snapshot = await database.Snapshots.AsNoTracking().SingleOrDefaultAsync(x => x.Id == snapshotId, cancellationToken);
        if (snapshot is null)
            return NotFound();

        var stored = await objectStore.GetAsync($"{snapshot.ContentPrefix}/screenshot.png", cancellationToken);
        if (stored is null)
            return NotFound();
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(stored.Content, "image/png");
    }

    private string[]? NormalizeTags(IEnumerable<string>? values)
    {
        var tags = (values ?? [])
            .Select(value => value.Trim().TrimStart('#').ToLowerInvariant())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (tags.Length > 30)
            ModelState.AddModelError(nameof(SnapshotSearchRequest.Tags), "At most 30 tags can be required.");

        if (tags.Any(tag => tag.Length > 100))
            ModelState.AddModelError(nameof(SnapshotSearchRequest.Tags), "Each tag must contain at most 100 characters.");

        return ModelState.IsValid ? tags : null;
    }

    private static string[] SplitTerms(string? value) => (value ?? string.Empty)
        .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string ContainsPattern(string value) => $"%{ExactPattern(value.Trim())}%";

    private static string ExactPattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}
