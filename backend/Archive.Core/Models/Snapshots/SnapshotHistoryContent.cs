using System.Text.Json;

namespace Archive.Core.Models.Snapshots;

/// <summary>Represents the stable history envelope while leaving every event payload open and versioned.</summary>
public sealed record SnapshotHistoryContent(string Type, int Version, JsonElement Data);
