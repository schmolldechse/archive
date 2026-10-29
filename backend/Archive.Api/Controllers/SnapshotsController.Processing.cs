using System.Text.Json;
using Microsoft.Extensions.Options;
using Archive.Api.Authentication;
using Archive.Api.Contracts.Snapshots.Processing;
using Archive.Api.Contracts.Snapshots.Creation;
using Archive.Api.Validation;
using Archive.Core.Models.Snapshots;
using Archive.Core.Models.Processing;
using Archive.Core.Processing;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Archive.Api.Controllers;

public sealed partial class SnapshotsController
{
    private JsonSerializerOptions EventJsonOptions =>
        HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

    // Both POST actions share a CORS policy; prefer this one for preflights without a Content-Type.
    [HttpPost(Order = -1), Consumes("application/json")]
    [EnableCors(ArchiveAuthenticationExtension.CorsPolicy)]
    [EndpointName("CreateSnapshotFromUrl"), EndpointSummary("Submits a URL for capture.")]
    [ProducesResponseType<SnapshotCreationResponse>(StatusCodes.Status202Accepted, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<ActionResult<SnapshotCreationResponse>> CreateFromUrlAsync([FromBody] CreateSnapshotUrlRequest request,
        CancellationToken cancellationToken)
    {
        return CreationResponse(await snapshotRepository.CreateFromUrlAsync(
            new(request.SourceUrl, request.Title, request.Description, request.Tags, CurrentUserId,
                request.IsPrivate), cancellationToken));
    }

    [HttpPost, Consumes("multipart/form-data")]
    [EnableCors(ArchiveAuthenticationExtension.CorsPolicy)]
    [EndpointName("CreateSnapshotFromFile"), EndpointSummary("Submits an HTML, MHTML or Webarchive import.")]
    [ProducesResponseType<SnapshotCreationResponse>(StatusCodes.Status202Accepted, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public async Task<ActionResult<SnapshotCreationResponse>> CreateFromFileAsync([FromForm] CreateSnapshotFileRequest request,
        CancellationToken cancellationToken)
    {
        return CreationResponse(await snapshotRepository.CreateFromFileAsync(
            new(request.SourceUrl, request.Title, request.Description, request.Tags, CurrentUserId,
                request.IsPrivate),
            new(request.File.FileName, request.File.Length, request.File.OpenReadStream), cancellationToken));
    }

    [HttpGet("{snapshotId:guid}/status")]
    [EnableCors(ArchiveAuthenticationExtension.CorsPolicy)]
    [EndpointName("GetSnapshotStatus"), EndpointSummary("Gets processing status and queue position.")]
    [ProducesResponseType<SnapshotProcessingResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<ActionResult<SnapshotProcessingResponse>> StatusAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var state = await ReadProcessingStateAsync(snapshotId, cancellationToken);
        return state is null ? NotFound() : Ok(mapper.ToResponse(state));
    }

    [HttpGet("{snapshotId:guid}/events")]
    [EnableCors(ArchiveAuthenticationExtension.CorsPolicy)]
    [EndpointName("StreamSnapshotEvents"), EndpointSummary("Streams transient processing state.")]
    [EndpointDescription("Unnamed SSE data events contain SnapshotProcessingResponse. Terminal events close the stream. Before opening, missing IDs return 404 and unavailable processing returns 503. After opening, named error events report these conditions and close the stream.")]
    [ProducesResponseType<string>(StatusCodes.Status200OK, "text/event-stream")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<IActionResult> EventsAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        var state = await ReadProcessingStateAsync(snapshotId, cancellationToken);
        if (state is null) return NotFound();
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Accel-Buffering"] = "no";
        string? lastPayload = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            var payload = JsonSerializer.Serialize(mapper.ToResponse(state), EventJsonOptions);
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
                state = await ReadProcessingStateAsync(snapshotId, cancellationToken);
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
    [EnableCors(ArchiveAuthenticationExtension.CorsPolicy)]
    [EndpointName("CancelSnapshot"), EndpointSummary("Requests cancellation before capture storage completes.")]
    [ProducesResponseType<SnapshotProcessingResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<ActionResult<SnapshotProcessingResponse>> CancelAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        if (!await snapshotRepository.CanCancelProcessingAsync(snapshotId, CurrentUserId, cancellationToken))
            return NotFound();
        var result = await processing.CancelAsync(snapshotId, cancellationToken);
        return result.Outcome switch
        {
            CancellationOutcome.Accepted => Ok(mapper.ToResponse(result.State!)),
            CancellationOutcome.Missing => NotFound(),
            _ => Problem(statusCode: 409, title: "Snapshot cannot be cancelled", detail: "Capture storage has completed or processing has ended.")
        };
    }

    private Guid? CurrentUserId => User.Identity?.IsAuthenticated == true ? User.ReadUserId() : null;

    /// <summary>
    /// Revalidates private visibility after reading state and removes the cancellation affordance for
    /// permission-based readers who are not the snapshot owner.
    /// </summary>
    private async Task<SnapshotProcessingState?> ReadProcessingStateAsync(Guid snapshotId,
        CancellationToken cancellationToken)
    {
        if (!await snapshotRepository.CanReadProcessingAsync(snapshotId, CurrentUserId, cancellationToken))
            return null;
        var state = await processing.GetByIdAsync(snapshotId, cancellationToken);
        if (state is not { IsPrivate: true }) return state;
        if (!await snapshotRepository.CanReadProcessingAsync(snapshotId, CurrentUserId, cancellationToken))
            return null;
        if (state.CanCancel && !await snapshotRepository.CanCancelProcessingAsync(
                snapshotId, CurrentUserId, cancellationToken))
            state = state with { CanCancel = false };
        return state;
    }

    private ActionResult<SnapshotCreationResponse> CreationResponse(SnapshotCreationResult result)
    {
        switch (result)
        {
            case SnapshotCreationResult.Created created:
                var publicSnapshotUrl = $"/api/snapshots/{created.Id}";
                var statusUrl = $"{publicSnapshotUrl}/status";
                return Accepted(statusUrl, new SnapshotCreationResponse
                {
                    Id = created.Id,
                    ModerationStatus = created.ModerationStatus,
                    IsPrivate = created.IsPrivate,
                    StatusUrl = statusUrl,
                    SnapshotUrl = created.IsPrivate
                        ? $"/api/users/snapshots/{created.Id}"
                        : publicSnapshotUrl
                });
            case SnapshotCreationResult.UploadTooLarge:
                return Problem(statusCode: 413, title: "File too large", detail: "The uploaded file exceeds the system limit.");
            case SnapshotCreationResult.AccountUnavailable:
                return Unauthorized();
            case SnapshotCreationResult.PrivateRequiresRegistration:
                return Problem(statusCode: StatusCodes.Status403Forbidden,
                    title: "Private snapshots require registration",
                    detail: "Complete account registration before creating a private snapshot.",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "private_snapshot_requires_registration"
                    });
            case SnapshotCreationResult.Invalid invalid:
                ModelState.AddErrors(invalid.Errors);
                return ValidationProblem(ModelState);
            default: throw new InvalidOperationException("Unknown snapshot creation result.");
        }
    }

    private async Task WriteStreamErrorAsync(int status, string title, CancellationToken token)
    {
        var error = new SnapshotProcessingErrorResponse { Status = status, Title = title };
        await Response.WriteAsync($"event: error\ndata: {JsonSerializer.Serialize(error, EventJsonOptions)}\n\n", token);
        await Response.Body.FlushAsync(token);
    }
}
