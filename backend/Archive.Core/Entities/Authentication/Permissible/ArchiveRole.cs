using NpgsqlTypes;
using System.Text.Json.Serialization;

namespace Archive.Core.Entities.Authentication.Permissible;

[PgName("role")]
public enum ArchiveRole
{
    [PgName("USER")]
    [JsonStringEnumMemberName("USER")]
    User,

    [PgName("ADMIN")]
    [JsonStringEnumMemberName("ADMIN")]
    Admin
}
