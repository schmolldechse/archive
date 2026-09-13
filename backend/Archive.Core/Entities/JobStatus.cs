using System.ComponentModel;
using System.Text.Json.Serialization;
using NpgsqlTypes;

namespace Archive.Core.Entities;

[PgName("job_status")]
[Description("Lifecycle status of an archive job.")]
public enum JobStatus
{
    [PgName("QUEUED")]
    [JsonStringEnumMemberName("QUEUED")]
    Queued,

    [PgName("RUNNING")]
    [JsonStringEnumMemberName("RUNNING")]
    Running,

    [PgName("COMPLETED")]
    [JsonStringEnumMemberName("COMPLETED")]
    Completed,

    [PgName("FAILED")]
    [JsonStringEnumMemberName("FAILED")]
    Failed,

    [PgName("CANCELLED")]
    [JsonStringEnumMemberName("CANCELLED")]
    Cancelled,

    [PgName("DISCARDED")]
    [JsonStringEnumMemberName("DISCARDED")]
    Discarded,

    [PgName("ABORTED")]
    [JsonStringEnumMemberName("ABORTED")]
    Aborted
}
