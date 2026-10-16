using System.Text.Json;
using System.Text.Json.Serialization;

namespace Archive.Api.Serialization;

internal static class ArchiveJson
{
    public static void Configure(JsonSerializerOptions options) =>
        options.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
}
