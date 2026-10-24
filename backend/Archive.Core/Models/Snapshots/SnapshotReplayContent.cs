namespace Archive.Core.Models.Snapshots;

/// <summary>The caller owns Content and must dispose it after serving the replay resource.</summary>
public sealed record SnapshotReplayContent(Stream Content, string ContentType, string Role);

public abstract record SnapshotReplayContentResult
{
    private SnapshotReplayContentResult() { }
    public sealed record Found(SnapshotReplayContent Resource) : SnapshotReplayContentResult;
    public sealed record NotFound : SnapshotReplayContentResult;
    public sealed record InvalidPath : SnapshotReplayContentResult;
}
