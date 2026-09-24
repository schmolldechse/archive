using System.Text.Json;
using System.Text.Json.Serialization;
using Archive.Api.Contracts;
using Archive.Core;
using Archive.Core.Entities;
using Archive.Core.Jobs;
using Archive.Core.Storage;
using Archive.Core.Uploads;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Archive.Api.Controllers;

[ApiController]
[Route("api/archive")]
[Tags("Archive")]
public sealed class ArchiveController(
    DataContext database,
    IArchiveObjectStore objectStore,
    UploadFormatSelector uploadFormatSelector,
    IConfiguration configuration) : ControllerBase
{
    private static readonly JsonSerializerOptions EventJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(null, allowIntegerValues: false) }
    };

    [HttpPost]
    [Consumes("application/json")]
    [EndpointName("CreateArchiveFromUrl")]
    [EndpointSummary("Creates an archive from a URL.")]
    [EndpointDescription("Accepts a public HTTP or HTTPS URL as JSON and queues a new archive.")]
    [ProducesResponseType<ArchiveResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ArchiveResponse>> CreateFromUrlAsync(
        [FromBody] CreateArchiveUrlRequest request,
        CancellationToken cancellationToken) =>
        CreateAsync(
            SourceType.Url,
            request.SourceUrl,
            null,
            null,
            request.Title,
            request.Description,
            request.Tags,
            cancellationToken);

    [HttpPost]
    [Consumes("multipart/form-data")]
    [EndpointName("CreateArchiveFromFile")]
    [EndpointSummary("Creates an archive from an HTML, MHTML or Webarchive file.")]
    [EndpointDescription("Accepts a .html, .mhtml or .webarchive file and its metadata as multipart form data and queues a new archive.")]
    [ProducesResponseType<ArchiveResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ArchiveResponse>> CreateFromFileAsync(
        [FromForm] CreateArchiveFileRequest request,
        CancellationToken cancellationToken) =>
        CreateAsync(
            SourceType.HtmlFile,
            null,
            request.OriginalLink,
            request.File,
            request.Title,
            request.Description,
            request.Tags,
            cancellationToken);

    private async Task<ActionResult<ArchiveResponse>> CreateAsync(
        SourceType sourceType,
        string? sourceUrl,
        string? originalLink,
        IFormFile? upload,
        string title,
        string? description,
        string[]? requestTags,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        sourceUrl = sourceUrl?.Trim() ?? string.Empty;
        originalLink = originalLink?.Trim() ?? string.Empty;
        title = title.Trim();
        description = description?.Trim() ?? string.Empty;

        if (requestTags is { Length: > 30 })
            return BadRequest("At most 30 tags can be assigned.");

        var tags = NormalizeTags(requestTags ?? []);
        if (tags is null)
            return BadRequest("Each tag must contain between 1 and 100 characters.");

        if (string.IsNullOrWhiteSpace(title))
            return BadRequest("A title is required.");

        string? originUrl = null;
        if (sourceType == SourceType.Url)
        {
            if (!UrlNormalizer.TryNormalizeSource(sourceUrl, out originUrl, out var error))
                return BadRequest(error);
        }
        else if (string.IsNullOrWhiteSpace(originalLink))
        {
            return BadRequest("An original page URL is required.");
        }
        else if (!UrlNormalizer.TryNormalizeSource(originalLink, out originUrl, out var originalLinkError))
        {
            return BadRequest(originalLinkError);
        }

        IUploadFormatProbe? uploadFormat = null;
        if (upload is not null)
        {
            if (upload.Length == 0)
                return BadRequest("Provide a non-empty .html, .mhtml or .webarchive file.");
            var maxUploadBytes = configuration.GetValue("Archive:MaxUploadBytes", 250_000_000L);
            if (upload.Length > maxUploadBytes)
                return StatusCode(StatusCodes.Status413PayloadTooLarge, "The uploaded file exceeds the system limit.");

            var prefix = new byte[Math.Min(upload.Length, 8_192)];
            await using (var probeStream = upload.OpenReadStream())
            {
                var read = 0;
                while (read < prefix.Length)
                {
                    var count = await probeStream.ReadAsync(prefix.AsMemory(read), cancellationToken);
                    if (count == 0)
                        break;
                    read += count;
                }
                uploadFormat = uploadFormatSelector.Select(upload.FileName, prefix.AsSpan(0, read));
            }
            if (uploadFormat is null)
                return BadRequest("The file format is unsupported or does not match its extension.");
            sourceType = uploadFormat.SourceType;
        }
        else if (sourceType != SourceType.Url)
        {
            return BadRequest("Provide a non-empty .html, .mhtml or .webarchive file.");
        }

        var job = new ArchiveJob
        {
            SourceType = sourceType,
            OriginUrl = originUrl,
            Title = title,
            Description = string.IsNullOrWhiteSpace(description) ? null : description,
            Status = JobStatus.Queued,
            ProgressStep = ProgressStep.SourceCheck,
            ProgressPercent = 0
        };
        job.Tags = tags.Select(value => new ArchiveJobTag { JobId = job.Id, Value = value }).ToList();

        var projectKey = originUrl is not null
            ? UrlNormalizer.ProjectKey(originUrl)
            : sourceType == SourceType.HtmlFile ? $"html:{job.Id:N}" : $"upload:{job.Id:N}";

        var project = await database.Projects.SingleOrDefaultAsync(x => x.ProjectKey == projectKey, cancellationToken);
        if (project is null)
        {
            project = new Project { ProjectKey = projectKey };
            database.Projects.Add(project);
        }

        if (upload is not null)
        {
            job.UploadedObjectKey = $"jobs/{job.Id:N}/source{uploadFormat!.Extension}";
            await using var stream = upload.OpenReadStream();
            await objectStore.PutAsync(job.UploadedObjectKey, stream, uploadFormat.ContentType, cancellationToken);
        }

        database.ArchiveJobs.Add(job);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (job.UploadedObjectKey is not null)
            {
                try
                {
                    if (!await database.ArchiveJobs.AsNoTracking().AnyAsync(x => x.Id == job.Id, CancellationToken.None))
                        await objectStore.DeleteAsync(job.UploadedObjectKey, CancellationToken.None);
                }
                catch { /* An ambiguous database result must preserve the upload for a possible queued job. */ }
            }
            throw;
        }

        return Ok(await BuildResponseAsync(job, cancellationToken));
    }

    [HttpGet("{archiveId:guid}")]
    [EndpointName("GetArchive")]
    [EndpointSummary("Gets an archive.")]
    [EndpointDescription("Returns the status, progress, and result data of an archive.")]
    [ProducesResponseType<ArchiveResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArchiveResponse>> GetAsync(Guid archiveId, CancellationToken cancellationToken)
    {
        var archive = await database.ArchiveJobs.Include(x => x.Tags).SingleOrDefaultAsync(x => x.Id == archiveId, cancellationToken);
        return archive is null ? NotFound() : Ok(await BuildResponseAsync(archive, cancellationToken));
    }

    [HttpGet("{archiveId:guid}/events")]
    [Produces("text/event-stream")]
    [EndpointName("StreamArchiveEvents")]
    [EndpointSummary("Streams archive changes.")]
    [EndpointDescription("Sends status changes as server-sent events until the archive reaches a terminal state.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task EventsAsync(Guid archiveId, CancellationToken cancellationToken)
    {
        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        string? lastPayload = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            var archive = await database.ArchiveJobs.AsNoTracking().Include(x => x.Tags).SingleOrDefaultAsync(x => x.Id == archiveId, cancellationToken);
            if (archive is null)
            {
                await WriteEventAsync("Archive not found.", cancellationToken);
                return;
            }

            var response = await BuildResponseAsync(archive, cancellationToken);
            var payload = JsonSerializer.Serialize(response, EventJsonOptions);
            if (!string.Equals(payload, lastPayload, StringComparison.Ordinal))
            {
                await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                lastPayload = payload;
            }

            if (archive.Status is JobStatus.Completed or JobStatus.Failed or JobStatus.Cancelled or JobStatus.Discarded or JobStatus.Aborted)
                return;

            await Task.Delay(500, cancellationToken);
        }
    }

    [HttpPost("{archiveId:guid}/cancel")]
    [EndpointName("CancelArchive")]
    [EndpointSummary("Cancels an archive.")]
    [EndpointDescription("Cancels a queued or running archive and returns its updated status.")]
    [ProducesResponseType<ArchiveResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ArchiveResponse>> CancelAsync(Guid archiveId, CancellationToken cancellationToken)
    {
        var completedAt = DateTimeOffset.UtcNow;
        var updated = await database.ArchiveJobs
            .Where(x => x.Id == archiveId && (x.Status == JobStatus.Queued || x.Status == JobStatus.Running))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, JobStatus.Cancelled)
                .SetProperty(x => x.ErrorMessage, "The archive was cancelled.")
                .SetProperty(x => x.CompletedAt, completedAt), cancellationToken);
        if (updated == 0)
        {
            var exists = await database.ArchiveJobs.AnyAsync(x => x.Id == archiveId, cancellationToken);
            return exists
                ? Conflict("This archive can no longer be cancelled.")
                : NotFound("Archive not found.");
        }

        var archive = await database.ArchiveJobs.Include(x => x.Tags).SingleAsync(x => x.Id == archiveId, cancellationToken);
        return Ok(await BuildResponseAsync(archive, cancellationToken));
    }

    private async Task<ArchiveResponse> BuildResponseAsync(ArchiveJob archive, CancellationToken cancellationToken)
    {
        int? queuePosition = archive.Status == JobStatus.Queued
            ? await database.ArchiveJobs.CountAsync(x => x.Status == JobStatus.Queued && x.CreatedAt < archive.CreatedAt, cancellationToken) + 1
            : null;
        var message = archive.Status switch
        {
            JobStatus.Queued => "Waiting for archiving",
            JobStatus.Running => archive.ProgressStep switch
            {
                ProgressStep.SourceCheck => "Checking source",
                ProgressStep.LoadingPage => "Loading page",
                ProgressStep.LoadingDynamicContent => "Loading dynamic content",
                ProgressStep.SavingSnapshot => "Saving snapshot",
                _ => "Archiving in progress"
            },
            JobStatus.Completed => "Archiving completed",
            JobStatus.Cancelled => "Archiving cancelled",
            JobStatus.Discarded => "Archiving discarded because of a system limit",
            JobStatus.Aborted => "Archiving aborted after restart",
            JobStatus.Failed => "Archiving failed",
            _ => "Unknown status"
        };

        var snapshot = archive.Status == JobStatus.Completed
            ? await database.Snapshots.AsNoTracking()
                .Where(x => x.Id == archive.Id)
                .Select(x => new { x.Id, x.Quality, x.DurationMilliseconds, x.StorageBytes, x.ResourceCount })
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        return new ArchiveResponse(
            archive.Id,
            archive.Status,
            archive.ProgressStep,
            archive.ProgressPercent,
            message,
            queuePosition,
            snapshot?.Id,
            snapshot?.Quality,
            archive.ErrorMessage,
            archive.CreatedAt,
            archive.CompletedAt,
            snapshot?.DurationMilliseconds ?? 0,
            snapshot?.StorageBytes ?? 0,
            snapshot?.ResourceCount ?? 0);
    }

    private async Task WriteEventAsync(object payload, CancellationToken cancellationToken)
    {
        await Response.WriteAsync($"data: {JsonSerializer.Serialize(payload, EventJsonOptions)}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private static IReadOnlyList<string>? NormalizeTags(IEnumerable<string> values)
    {
        var tags = values
            .Select(value => value.Trim().TrimStart('#').ToLowerInvariant())
            .ToArray();

        if (tags.Any(value => value.Length is 0 or > 100))
            return null;

        return tags
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
