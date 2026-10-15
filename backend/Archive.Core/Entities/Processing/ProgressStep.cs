using System.ComponentModel;
using System.Text.Json.Serialization;
using NpgsqlTypes;

namespace Archive.Core.Entities.Processing;

[PgName("progress_step")]
[Description("Temporary processing step of a snapshot.")]
public enum ProgressStep
{
    [PgName("SOURCE_CHECK")]
    [JsonStringEnumMemberName("SOURCE_CHECK")]
    SourceCheck,

    [PgName("LOADING_PAGE")]
    [JsonStringEnumMemberName("LOADING_PAGE")]
    LoadingPage,

    [PgName("LOADING_DYNAMIC_CONTENT")]
    [JsonStringEnumMemberName("LOADING_DYNAMIC_CONTENT")]
    LoadingDynamicContent,

    [PgName("SAVING_SNAPSHOT")]
    [JsonStringEnumMemberName("SAVING_SNAPSHOT")]
    SavingSnapshot,

    [PgName("FINISHED")]
    [JsonStringEnumMemberName("FINISHED")]
    Finished
}
