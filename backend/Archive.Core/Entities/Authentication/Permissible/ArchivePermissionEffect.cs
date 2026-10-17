using NpgsqlTypes;
using System.Text.Json.Serialization;

namespace Archive.Core.Entities.Authentication.Permissible;

[PgName("permission_effect")]
public enum ArchivePermissionEffect
{
    [PgName("ALLOW")]
    [JsonStringEnumMemberName("ALLOW")]
    Allow,

    [PgName("DENY")]
    [JsonStringEnumMemberName("DENY")]
    Deny
}
