namespace Archive.Core.Models.Archive;

public abstract record ArchiveCancellationResult
{
    private ArchiveCancellationResult() { }
    public sealed record Cancelled(ArchiveStatus Archive) : ArchiveCancellationResult;
    public sealed record NotFound : ArchiveCancellationResult;
    public sealed record NotCancellable : ArchiveCancellationResult;
}
