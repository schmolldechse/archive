using System.Text.Json;
using System.Text.Json.Serialization;
using Archive.Core.Models.Snapshots;
using Archive.Core.Models.Snapshots.History;

namespace Archive.Core.Serialization;

/// <summary>Owns the versioned JSONB boundary for strongly typed snapshot history events.</summary>
internal static class SnapshotHistorySerializer
{
    private const int MaxContentBytes = 32 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    /// <summary>Wraps a concrete event payload with metadata supplied by its abstract base type.</summary>
    internal static JsonElement Serialize(SnapshotHistoryData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        ValidateMetadata(data);
        var content = new SnapshotHistoryContent(data.Type, data.Version,
            JsonSerializer.SerializeToElement(data, data.GetType(), JsonOptions));
        var bytes = JsonSerializer.SerializeToUtf8Bytes(content, JsonOptions);
        if (bytes.Length > MaxContentBytes)
            throw new InvalidOperationException("Snapshot history content exceeds its limit.");

        return JsonSerializer.Deserialize<JsonElement>(bytes);
    }

    /// <summary>Reads the open history envelope without interpreting an unknown event payload.</summary>
    internal static SnapshotHistoryContent Deserialize(JsonElement content)
    {
        var result = content.Deserialize<SnapshotHistoryContent>(JsonOptions)
            ?? throw new JsonException("Snapshot history content is missing.");
        ValidateContent(result);
        return result;
    }

    /// <summary>Reads a known event payload after verifying its stable type and schema version.</summary>
    internal static TData Deserialize<TData>(JsonElement content)
        where TData : SnapshotHistoryData
    {
        var envelope = Deserialize(content);
        var data = envelope.Data.Deserialize<TData>(JsonOptions)
            ?? throw new JsonException("Snapshot history payload is missing.");
        ValidateMetadata(data);
        if (envelope.Type != data.Type || envelope.Version != data.Version)
            throw new JsonException($"Snapshot history content does not match {data.Type} version {data.Version}.");

        return data;
    }

    private static void ValidateMetadata(SnapshotHistoryData data)
    {
        if (string.IsNullOrWhiteSpace(data.Type) || data.Type.Length > 128 || data.Version <= 0)
            throw new JsonException("Snapshot history requires a valid type and positive schema version.");
    }

    private static void ValidateContent(SnapshotHistoryContent content)
    {
        if (string.IsNullOrWhiteSpace(content.Type) || content.Type.Length > 128 || content.Version <= 0
            || content.Data.ValueKind != JsonValueKind.Object)
            throw new JsonException("Snapshot history content is invalid.");
    }
}
