namespace Archive.Core.Models.Snapshots.History;

/// <summary>Stable event identifiers shared by persistence, projections and API presentation.</summary>
public static class SnapshotHistoryTypes
{
    public const string Submitted = "snapshot.submitted";
    public const string Stored = "snapshot.stored";
    public const string ProcessingFinished = "snapshot.processing_finished";
    public const string Moderated = "snapshot.moderated";
    public const string Resubmitted = "snapshot.resubmitted";
    public const string DetailsUpdated = "snapshot.details_updated";
    public const string SourceUpdated = "snapshot.source_updated";
    public const string AttributionUpdated = "snapshot.attribution_updated";
    public const string VisibilityUpdated = "snapshot.visibility_updated";
}
