using System.ComponentModel;
using System.Text.Json.Serialization;
using NpgsqlTypes;

namespace Archive.Core.Entities;

[PgName("source_type")]
[Description("Source used to create an archive job.")]
public enum SourceType
{
    [PgName("URL")]
    [JsonStringEnumMemberName("URL")]
    Url,

    [PgName("HTML")]
    [JsonStringEnumMemberName("HTML")]
    HtmlFile,

    [PgName("MHTML")]
    [JsonStringEnumMemberName("MHTML")]
    MhtmlFile,

    [PgName("WEBARCHIVE")]
    [JsonStringEnumMemberName("WEBARCHIVE")]
    WebArchiveFile
}
