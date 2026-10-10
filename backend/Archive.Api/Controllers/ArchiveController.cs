using Archive.Api.Contracts;
using Archive.Api.Contracts.Archive.Creation;
using Archive.Core;
using Archive.Core.Entities;
using Archive.Core.Jobs;
using Archive.Core.Options;
using Archive.Core.Storage;
using Archive.Core.Uploads;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Archive.Api.Controllers;

[ApiController]
[Route("api/archive")]
[Tags("Archive")]
public sealed class ArchiveController(
    DataContext database,
    IArchiveObjectStore objectStore,
    UploadFormatSelector uploadFormatSelector,
    ArchiveOptions options) : ControllerBase
{
    private static readonly JsonSerializerOptions EventJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(null, allowIntegerValues: false) }
    };

    [HttpPost]
    [Consumes("application/json")]
    [EndpointName("CreateArchiveFromUrl")]
    [EndpointSummary("Creates an archive from a URL.")]
    [EndpointDescription("Queues a public HTTP or HTTPS URL. Returns the job status immediately; snapshot creation runs asynchronously.")]
    [ProducesResponseType<ArchiveResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public async Task<ActionResult<ArchiveResponse>> CreateFromUrlAsync(
        [FromBody] CreateArchiveUrlRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        return await CreateAsync(
            SourceType.Url,
            request.SourceUrl,
            null,
            request.Title,
            request.Description,
            request.Tags,
            cancellationToken);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [EndpointName("CreateArchiveFromFile")]
    [EndpointSummary("Creates an archive from an HTML, MHTML or Webarchive file.")]
    [EndpointDescription("Accepts a .html, .mhtml or .webarchive file and its metadata as multipart form data and queues a new archive.")]
    [ProducesResponseType<ArchiveResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public async Task<ActionResult<ArchiveResponse>> CreateFromFileAsync(
        [FromForm] CreateArchiveFileRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        return await CreateAsync(
            SourceType.HtmlFile,
            request.OriginalLink,
            request.File,
            request.Title,
            request.Description,
            request.Tags,
            cancellationToken);
    }

    private async Task<ActionResult<ArchiveResponse>> CreateAsync(
        SourceType sourceType,
        string sourceUrl,
        IFormFile? upload,
        string title,
        string? description,
        string[]? requestTags,
        CancellationToken cancellationToken)
    {
        title = title.Trim();
        description = description?.Trim() ?? string.Empty;

        var tags = (requestTags ?? [])
            .Select(ArchiveRequestValidation.NormalizeTag)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (!UrlNormalizer.TryNormalizeSource(sourceUrl, out var originUrl, out var error))
            return InvalidField(sourceType == SourceType.Url ? "sourceUrl" : "originalLink", error);

        IUploadFormatProbe? uploadFormat = null;
        if (upload is not null)
        {
            if (upload.Length > options.MaxUploadBytes)
                return Problem(statusCode: StatusCodes.Status413PayloadTooLarge,
                    title: "File too large", detail: "The uploaded file exceeds the system limit.");

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
                return InvalidField("file", "The file format is unsupported or does not match its extension.");
            sourceType = uploadFormat.SourceType;
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

        var projectKey = UrlNormalizer.ProjectKey(originUrl);

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
    [ProducesResponseType<ArchiveResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<ArchiveResponse>> GetAsync(Guid archiveId, CancellationToken cancellationToken)
    {
        var archive = await database.ArchiveJobs.Include(x => x.Tags).SingleOrDefaultAsync(x => x.Id == archiveId, cancellationToken);
        return archive is null ? NotFound() : Ok(await BuildResponseAsync(archive, cancellationToken));
    }

    [HttpGet("{archiveId:guid}/events")]
    [EndpointName("StreamArchiveEvents")]
    [EndpointSummary("Streams archive changes.")]
    [EndpointDescription("Streams unnamed SSE events whose data is a JSON archive response. The stream closes when the job is completed, failed, cancelled, discarded or aborted. An unknown archive sends a JSON string error and closes the stream; its HTTP status is 200. Use curl -N to consume events without output buffering.")]
    [ProducesResponseType<string>(StatusCodes.Status200OK, "text/event-stream")]
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
    [ProducesResponseType<ArchiveResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
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
                ? Problem(statusCode: StatusCodes.Status409Conflict, title: "Archive cannot be cancelled",
                    detail: "Only queued or running archives can be cancelled.")
                : Problem(statusCode: StatusCodes.Status404NotFound, title: "Archive not found");
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

    private ActionResult InvalidField(string field, string message)
    {
        ModelState.AddModelError(field, message);
        return ValidationProblem(ModelState);
    }
}
