namespace Archive.Worker.Capture.Network;

/// <summary>Determines whether a browser capture may contact an absolute network URL.</summary>
public interface ICaptureNetworkGuard
{
    Task<bool> IsAllowedAsync(string url, CancellationToken cancellationToken);
}
