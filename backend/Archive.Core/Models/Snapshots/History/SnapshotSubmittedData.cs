namespace Archive.Core.Models.Snapshots.History;

internal sealed record SnapshotSubmittedData() : SnapshotHistoryData
{
    internal override string Type => SnapshotHistoryTypes.Submitted;
}
