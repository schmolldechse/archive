using System.Text.Json.Serialization;

namespace Archive.SnapshotMigrator;

[JsonConverter(typeof(JsonStringEnumConverter<SnapshotMigrationState>))]
internal enum SnapshotMigrationState
{
    Current,
    Migratable,
    Pending,
    Restored,
    Blocked,
    Inconsistent,
    Missing
}

internal sealed record SnapshotMigrationAssessment(
    Guid SnapshotId,
    SnapshotMigrationState State,
    string Message,
    string? ContentPrefix);

internal sealed record SnapshotMigrationResult(
    Guid SnapshotId,
    string Action,
    bool Succeeded,
    string Message);

internal sealed record SnapshotMigrationJournal
{
    public const string Prepared = "prepared";
    public const string Applied = "applied";
    public const string Restored = "restored";

    public required Guid SnapshotId { get; init; }
    public required Guid MigrationId { get; init; }
    public required string Status { get; init; }
    public required string OriginalContentPrefix { get; init; }
    public required string MigratedContentPrefix { get; init; }
    public long? OriginalStorageBytes { get; init; }
    public long MigratedStorageBytes { get; init; }
    public int? OriginalResourceCount { get; init; }
    public int MigratedResourceCount { get; init; }
    public required IReadOnlyList<string> OriginalObjectKeys { get; init; }
    public required IReadOnlyList<string> MigratedObjectKeys { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? AppliedAt { get; init; }
    public DateTimeOffset? RestoredAt { get; init; }
}

internal sealed record LegacySnapshotMetadata(
    IReadOnlyDictionary<string, LegacySnapshotResource> Resources,
    IReadOnlyList<string> MissingUrls,
    int MissingUrlCount,
    bool Incomplete,
    string? OriginalUrl);

internal sealed record LegacySnapshotResource(string? SourceUrl, string? ContentType);

[JsonSerializable(typeof(SnapshotMigrationJournal))]
[JsonSerializable(typeof(SnapshotMigrationAssessment[]))]
[JsonSerializable(typeof(SnapshotMigrationResult[]))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
internal partial class SnapshotMigrationJsonContext : JsonSerializerContext;
