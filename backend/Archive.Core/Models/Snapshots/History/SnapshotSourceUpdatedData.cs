using Archive.Core.Entities;

namespace Archive.Core.Models.Snapshots.History;

/// <summary>Records a provenance URL correction and any resulting moderation transition.</summary>
internal sealed record SnapshotSourceUpdatedData(
    string? PreviousSourceUrl,
    string SourceUrl,
    SnapshotModerationStatus PreviousStatus,
    SnapshotModerationStatus Status,
    bool ReviewRequired) : SnapshotHistoryData
{
    internal override string Type => SnapshotHistoryTypes.SourceUpdated;
}
