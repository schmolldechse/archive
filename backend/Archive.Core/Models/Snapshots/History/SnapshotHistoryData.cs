namespace Archive.Core.Models.Snapshots.History;

/// <summary>Defines an event's identity and schema version independently of its payload fields.</summary>
public abstract record SnapshotHistoryData
{
    /// <summary>The stable event identifier supplied by the concrete event model.</summary>
    internal abstract string Type { get; }

    /// <summary>The payload schema version; new event models use version one unless they explicitly override it.</summary>
    internal virtual int Version => 1;
}
