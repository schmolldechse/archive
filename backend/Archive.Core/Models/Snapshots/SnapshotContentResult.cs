namespace Archive.Core.Models.Snapshots;

public abstract record SnapshotContentResult
{
    private SnapshotContentResult() { }
    public sealed record Found(SnapshotContent Resource) : SnapshotContentResult;
    public sealed record NotFound : SnapshotContentResult;
    public sealed record InvalidPath : SnapshotContentResult;
}
