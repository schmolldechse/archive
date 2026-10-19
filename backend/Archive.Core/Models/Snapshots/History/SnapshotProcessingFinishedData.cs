using Archive.Core.Entities.Processing;

namespace Archive.Core.Models.Snapshots.History;

/// <summary>Version-one terminal capture outcome without diagnostic secrets.</summary>
internal sealed record SnapshotProcessingFinishedData() : SnapshotHistoryData
{
    internal override string Type => "snapshot.processing_finished";
}
