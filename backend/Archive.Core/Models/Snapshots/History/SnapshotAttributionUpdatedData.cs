namespace Archive.Core.Models.Snapshots.History;

/// <summary>
/// Stores durable user identifiers for an attribution change. API projections resolve them to current public profiles.
/// </summary>
internal sealed record SnapshotAttributionUpdatedData(
    Guid? PreviousUserId,
    Guid? UserId) : SnapshotHistoryData
{
    internal override string Type => SnapshotHistoryTypes.AttributionUpdated;
}
