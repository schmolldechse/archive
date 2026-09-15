using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Archive.Api.Contracts;

[Description("Capture-time ordering, with the snapshot ID as a stable tie-breaker.")]
public enum SnapshotOrder
{
    [JsonStringEnumMemberName("desc")]
    Desc,

    [JsonStringEnumMemberName("asc")]
    Asc
}
