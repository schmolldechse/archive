using Archive.Core.Entities.Processing;

namespace Archive.Core.Models.Snapshots.History;

/// <summary>Version-two terminal capture outcome without diagnostic secrets.</summary>
internal sealed record SnapshotProcessingFinishedData(ProcessingStatus Status) : SnapshotHistoryData
{
    internal override string Type => SnapshotHistoryTypes.ProcessingFinished;
    internal override int Version => 2;
}
