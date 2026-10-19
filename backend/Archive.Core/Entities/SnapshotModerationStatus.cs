using System.ComponentModel;
using System.Text.Json.Serialization;
using NpgsqlTypes;

namespace Archive.Core.Entities;

[PgName("snapshot_moderation_status")]
[Description("Review state of a submitted snapshot.")]
public enum SnapshotModerationStatus
{
    [PgName("PENDING")]
    [JsonStringEnumMemberName("PENDING")] 
    Pending,
    
    [PgName("APPROVED")] 
    [JsonStringEnumMemberName("APPROVED")] 
    Approved,
    
    [PgName("REJECTED")]
    [JsonStringEnumMemberName("REJECTED")] 
    Rejected,
    
    [PgName("RESUBMITTED")]
    [JsonStringEnumMemberName("RESUBMITTED")] 
    Resubmitted
}
