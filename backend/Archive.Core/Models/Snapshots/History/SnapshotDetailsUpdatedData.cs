using System.Text.Json.Serialization;

namespace Archive.Core.Models.Snapshots.History;

/// <summary>Identifies user-editable metadata changed by one atomic update.</summary>
internal sealed record SnapshotDetailsUpdatedData(
    IReadOnlyList<SnapshotDetailField> Fields,
    IReadOnlyList<string> AddedTags,
    IReadOnlyList<string> RemovedTags,
    SnapshotTextChange? Title,
    SnapshotTextChange? Description,
    SnapshotTagsChange? Tags) : SnapshotHistoryData
{
    internal override string Type => SnapshotHistoryTypes.DetailsUpdated;

    internal override int Version => 2;
}

/// <summary>Stores the before and after values of one editable text field.</summary>
internal sealed record SnapshotTextChange(string? PreviousValue, string? UpdatedValue);

/// <summary>Stores the complete before and after tag sets for one metadata edit.</summary>
internal sealed record SnapshotTagsChange(
    IReadOnlyList<string> PreviousValues,
    IReadOnlyList<string> UpdatedValues);

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
