using System.Text.Json.Serialization;

namespace Archive.Core.Models.Snapshots.History;

/// <summary>Identifies user-editable metadata changed by one atomic update.</summary>
internal sealed record SnapshotDetailsUpdatedData(
    IReadOnlyList<SnapshotDetailField> Fields,
    IReadOnlyList<string> AddedTags,
    IReadOnlyList<string> RemovedTags) : SnapshotHistoryData
{
    internal override string Type => SnapshotHistoryTypes.DetailsUpdated;
}

/// <summary>Stable metadata field identifiers used by snapshot history.</summary>
public enum SnapshotDetailField
{
    [JsonStringEnumMemberName("TITLE")]
    Title,

    [JsonStringEnumMemberName("DESCRIPTION")]
    Description,

    [JsonStringEnumMemberName("TAGS")]
    Tags
}
