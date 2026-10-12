namespace Archive.Core.Models.Snapshots;

public abstract record SnapshotSearchResult
{
    private SnapshotSearchResult() { }
    public sealed record Found(SnapshotPage Page) : SnapshotSearchResult;
    public sealed record Invalid(IReadOnlyList<FieldValidationError> Errors) : SnapshotSearchResult;
}
