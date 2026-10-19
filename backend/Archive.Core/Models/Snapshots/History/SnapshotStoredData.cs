using Archive.Core.Entities;

namespace Archive.Core.Models.Snapshots.History;

/// <summary>Version-one confirmed capture summary, independent of public availability.</summary>
internal sealed record SnapshotStoredData(
    SnapshotQuality? Quality, 
    long? DurationMilliseconds, 
    long? StorageBytes, 
    int? ResourceCount) : SnapshotHistoryData
{
    internal override string Type => "snapshot.stored";
}
