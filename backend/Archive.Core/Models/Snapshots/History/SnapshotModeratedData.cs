using Archive.Core.Entities;

namespace Archive.Core.Models.Snapshots.History;

/// <summary>Version-one decision payload for manual, automatic and legacy approvals.</summary>
internal sealed record SnapshotModeratedData(
    SnapshotModerationStatus? PreviousStatus, 
    SnapshotModerationStatus Status, 
    string? Reason) : SnapshotHistoryData
{
    internal override string Type => "snapshot.moderated";
}
