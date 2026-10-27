namespace Archive.Core.Models.Snapshots;

/// <summary>Result of a conditional owner or administrative snapshot deletion.</summary>
public abstract record SnapshotDeletionResult
{
    private SnapshotDeletionResult() { }

    public sealed record Deleted : SnapshotDeletionResult;
    public sealed record NotFound : SnapshotDeletionResult;
    public sealed record Forbidden : SnapshotDeletionResult;
    public sealed record Stale : SnapshotDeletionResult;
}
