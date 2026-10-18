using Archive.Core.Entities;

namespace Archive.Core.Models.Snapshots.History;

/// <summary>Version-one request to review the unchanged archived record again.</summary>
internal sealed record SnapshotResubmittedData(
    SnapshotModerationStatus PreviousStatus, 
    SnapshotModerationStatus Status) : SnapshotHistoryData
{
    internal override string Type => "snapshot.resubmitted";
}
