using Archive.Api.Contracts;
using Archive.Api.Contracts.Archive;
using Archive.Core.Models.Archive;
using Archive.Core.Repositories.ArchiveRepository;
using Archive.Api.Contracts.Archive.Creation;
using Archive.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Archive.Api.Controllers;

[ApiController]
[Route("api/archive")]
[Tags("Archive")]
public sealed class ArchiveController(IArchiveRepository archiveRepository) : ControllerBase
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

        return CreationResponse(await archiveRepository.CreateFromUrlAsync(
            new(request.SourceUrl, request.Title, request.Description, request.Tags), cancellationToken), "sourceUrl");
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

        return CreationResponse(await archiveRepository.CreateFromFileAsync(
            new(request.OriginalLink, request.Title, request.Description, request.Tags),
            new(request.File.FileName, request.File.Length, request.File.OpenReadStream), cancellationToken), "originalLink");
    }

    [HttpGet("{archiveId:guid}")]
    [EndpointName("GetArchive")]
    [EndpointSummary("Gets an archive.")]
    [EndpointDescription("Returns the status, progress, and result data of an archive.")]
    [ProducesResponseType<ArchiveResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<ArchiveResponse>> GetAsync(Guid archiveId, CancellationToken cancellationToken)
    {
        var archive = await archiveRepository.GetByIdAsync(archiveId, cancellationToken);
        return archive is null ? NotFound() : Ok(archive.ToResponse());
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
            var archive = await archiveRepository.GetByIdAsync(archiveId, cancellationToken);
            if (archive is null)
            {
                await WriteEventAsync("Archive not found.", cancellationToken);
                return;
            }

            var response = archive.ToResponse();
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
        return await archiveRepository.CancelAsync(archiveId, cancellationToken) switch
        {
            ArchiveCancellationResult.Cancelled result => Ok(result.Archive.ToResponse()),
            ArchiveCancellationResult.NotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Archive not found"),
            ArchiveCancellationResult.NotCancellable => Problem(statusCode: StatusCodes.Status409Conflict, title: "Archive cannot be cancelled",
                detail: "Only queued or running archives can be cancelled."),
            _ => throw new InvalidOperationException("Unknown archive cancellation result.")
        };
    }

    private ActionResult<ArchiveResponse> CreationResponse(ArchiveCreationResult result, string originField)
    {
        switch (result)
        {
            case ArchiveCreationResult.Created created:
                return Ok(created.Archive.ToResponse());
            case ArchiveCreationResult.UploadTooLarge:
                return Problem(statusCode: StatusCodes.Status413PayloadTooLarge,
                    title: "File too large", detail: "The uploaded file exceeds the system limit.");
            case ArchiveCreationResult.Invalid invalid:
                foreach (var error in invalid.Errors)
                    ModelState.AddModelError(error.Field == "originUrl" ? originField : error.Field, error.Message);
                return ValidationProblem(ModelState);
            default:
                throw new InvalidOperationException("Unknown archive creation result.");
        }
    }

    private async Task WriteEventAsync(object payload, CancellationToken cancellationToken)
    {
        await Response.WriteAsync($"data: {JsonSerializer.Serialize(payload, EventJsonOptions)}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

}
