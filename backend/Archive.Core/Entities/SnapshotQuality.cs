using System.ComponentModel;
using System.Text.Json.Serialization;
using NpgsqlTypes;

namespace Archive.Core.Entities;

[PgName("snapshot_quality")]
[Description("Completeness of an archived snapshot.")]
public enum SnapshotQuality
{
    [PgName("COMPLETE")]
    [JsonStringEnumMemberName("COMPLETE")]
    Complete,

    [PgName("INCOMPLETE")]
    [JsonStringEnumMemberName("INCOMPLETE")]
    Incomplete
}
