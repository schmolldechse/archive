using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Archive.Core.Models.Snapshots;

[Description("Capture-time ordering, with the snapshot ID as a stable tie-breaker.")]
public enum SnapshotSortOrder
{
    [JsonStringEnumMemberName("desc")]
    Desc,

    [JsonStringEnumMemberName("asc")]
    Asc
}
