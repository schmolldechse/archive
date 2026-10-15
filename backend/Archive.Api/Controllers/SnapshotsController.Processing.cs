using System.Text.Json;
using System.Text.Json.Serialization;
using Archive.Api.Authentication;
using Archive.Api.Contracts;
using Archive.Api.Contracts.Snapshots.Creation;
using Archive.Core.Models.Snapshots;
using Archive.Core.Models.Processing;
using Archive.Core.Processing;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Archive.Api.Controllers;

public sealed partial class SnapshotsController
{
    private static readonly JsonSerializerOptions EventJsonOptions = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter(null, allowIntegerValues: false) } };

    [HttpPost, Consumes("application/json")]
    [EnableCors(ArchiveAuthenticationExtension.CorsPolicy)]
    [EndpointName("CreateSnapshotFromUrl"), EndpointSummary("Submits a URL for capture.")]
    [ProducesResponseType<SnapshotReceipt>(StatusCodes.Status202Accepted, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<ActionResult<SnapshotReceipt>> CreateFromUrlAsync([FromBody] CreateSnapshotUrlRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return CreationResponse(await snapshotRepository.CreateFromUrlAsync(
            new(request.SourceUrl, request.Title, request.Description, request.Tags, CurrentUserId), cancellationToken), "sourceUrl");
    }

    [HttpPost, Consumes("multipart/form-data")]
    [EnableCors(ArchiveAuthenticationExtension.CorsPolicy)]
    [EndpointName("CreateSnapshotFromFile"), EndpointSummary("Submits an HTML, MHTML or Webarchive import.")]
    [ProducesResponseType<SnapshotReceipt>(StatusCodes.Status202Accepted, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public async Task<ActionResult<SnapshotReceipt>> CreateFromFileAsync([FromForm] CreateSnapshotFileRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return CreationResponse(await snapshotRepository.CreateFromFileAsync(
            new(request.OriginalLink, request.Title, request.Description, request.Tags, CurrentUserId),
            new(request.File.FileName, request.File.Length, request.File.OpenReadStream), cancellationToken), "originalLink");
    }

    [HttpGet("{snapshotId:guid}/status")]
    [EndpointName("GetSnapshotStatus"), EndpointSummary("Gets processing status and queue position.")]
    [ProducesResponseType<SnapshotProcessingResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<ActionResult<SnapshotProcessingResponse>> StatusAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var state = await processing.GetByIdAsync(snapshotId, cancellationToken);
        return state is null ? NotFound() : Ok(SnapshotProcessingResponse.From(state));
    }

    [HttpGet("{snapshotId:guid}/events")]
    [EndpointName("StreamSnapshotEvents"), EndpointSummary("Streams transient processing state.")]
    [EndpointDescription("Unnamed SSE data events contain SnapshotProcessingResponse. Terminal events close the stream. Before opening, missing IDs return 404 and unavailable processing returns 503. After opening, named error events report these conditions and close the stream.")]
    [ProducesResponseType<string>(StatusCodes.Status200OK, "text/event-stream")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<IActionResult> EventsAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        var state = await processing.GetByIdAsync(snapshotId, cancellationToken);
        if (state is null) return NotFound();
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Accel-Buffering"] = "no";
        string? lastPayload = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            var payload = JsonSerializer.Serialize(SnapshotProcessingResponse.From(state), EventJsonOptions);
            if (payload != lastPayload)
            {
                await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                lastPayload = payload;
            }
            else
            {
                await Response.WriteAsync(": heartbeat\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
            if (state.IsTerminal) break;
            await Task.Delay(500, cancellationToken);
            try
            {
                state = await processing.GetByIdAsync(snapshotId, cancellationToken);
                if (state is not null) continue;
                await WriteStreamErrorAsync(404, "Snapshot processing state not found", cancellationToken);
            }
            catch (Exception exception) when (ProcessingFailure.IsUnavailable(exception))
            { await WriteStreamErrorAsync(503, "Processing service unavailable", cancellationToken); }
            break;
        }
        return new EmptyResult();
    }

    [HttpPost("{snapshotId:guid}/cancel")]
    [EndpointName("CancelSnapshot"), EndpointSummary("Requests cancellation before publication.")]
    [ProducesResponseType<SnapshotProcessingResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<ActionResult<SnapshotProcessingResponse>> CancelAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        var result = await processing.CancelAsync(snapshotId, cancellationToken);
        return result.Outcome switch
        {
            CancellationOutcome.Accepted => Ok(SnapshotProcessingResponse.From(result.State!)),
            CancellationOutcome.Missing => NotFound(),
            _ => Problem(statusCode: 409, title: "Snapshot cannot be cancelled", detail: "Publication has started or processing has ended.")
        };
    }

    private Guid? CurrentUserId => User.Identity?.IsAuthenticated == true ? User.ReadUserId() : null;

    private ActionResult<SnapshotReceipt> CreationResponse(SnapshotCreationResult result, string originField)
    {
        switch (result)
        {
            case SnapshotCreationResult.Created created:
                var snapshotUrl = $"/api/snapshots/{created.Id}";
                var statusUrl = $"{snapshotUrl}/status";
                return Accepted(statusUrl, new SnapshotReceipt(created.Id, statusUrl, snapshotUrl));
            case SnapshotCreationResult.UploadTooLarge:
                return Problem(statusCode: 413, title: "File too large", detail: "The uploaded file exceeds the system limit.");
            case SnapshotCreationResult.Invalid invalid:
                foreach (var error in invalid.Errors)
                    ModelState.AddModelError(error.Field == "originUrl" ? originField : error.Field, error.Message);
                return ValidationProblem(ModelState);
            default: throw new InvalidOperationException("Unknown snapshot creation result.");
        }
    }

    private async Task WriteStreamErrorAsync(int status, string title, CancellationToken token)
    {
        await Response.WriteAsync($"event: error\ndata: {JsonSerializer.Serialize(new { status, title })}\n\n", token);
        await Response.Body.FlushAsync(token);
    }
}
