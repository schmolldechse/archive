namespace Archive.Core.Models.Snapshots.History;

/// <summary>Records an owner-controlled change to public visibility.</summary>
internal sealed record SnapshotVisibilityUpdatedData(
    bool PreviousIsPrivate,
    bool IsPrivate) : SnapshotHistoryData
{
    internal override string Type => SnapshotHistoryTypes.VisibilityUpdated;
}
