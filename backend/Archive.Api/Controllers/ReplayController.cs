using Archive.Api.Replay;
using Archive.Core.Models.Snapshots;
using Archive.Core.Repositories.SnapshotRepository;
using Microsoft.AspNetCore.Mvc;

namespace Archive.Api.Controllers;

[ApiController]
[Route("replay/snapshots")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class ReplayController(
    ISnapshotRepository snapshots,
    ReplayClientCapabilityPolicy clients,
    ReplayContentPolicy policies) : ControllerBase
{
    /// <summary>Serves one manifest-authorized resource from an approved replay-compatible snapshot.</summary>
    [AcceptVerbs("GET", "HEAD", "OPTIONS")]
    [Route("{snapshotId:guid}/{**path}")]
    public async Task<IActionResult> ContentAsync(Guid snapshotId, string? path, CancellationToken cancellationToken)
    {
        if (HttpMethods.IsOptions(Request.Method))
        {
            Response.Headers.AccessControlAllowOrigin = "*";
            Response.Headers.AccessControlAllowMethods = "GET, HEAD, OPTIONS";
            Response.Headers.AccessControlAllowHeaders = "Content-Type";
            return NoContent();
        }

        var result = await snapshots.OpenReplayContentAsync(snapshotId, path, cancellationToken);
        if (result is SnapshotReplayContentResult.NotFound)
            return NotFound();
        if (result is SnapshotReplayContentResult.InvalidPath)
            return BadRequest();

        var resource = ((SnapshotReplayContentResult.Found)result).Resource;
        try
        {
            if (!policies.IsAllowedDestination(resource.Role, resource.ContentType,
                    Request.Headers["Sec-Fetch-Dest"].ToString()))
                return NotFound();

            var interactive = clients.AllowsInteraction(Request.Headers["Sec-CH-UA"].ToString());
            policies.Apply(Response, snapshotId, resource.Role, resource.ContentType, interactive);
            return File(resource.Content, resource.ContentType, enableRangeProcessing: false);
        }
        catch
        {
            await resource.Content.DisposeAsync();
            throw;
        }
    }
}
