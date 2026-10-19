using System.Text.Json;
using Archive.Core.Entities;
using Riok.Mapperly.Abstractions;

namespace Archive.Core.Mapping;

/// <summary>Maps persistence fields without interpreting or converting history content.</summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
internal static partial class SnapshotHistoryMapper
{
    /// <summary>Leaves identity generation to the entity and sequence and timestamp assignment to the repository.</summary>
    [MapperIgnoreTarget(nameof(SnapshotHistoryEntry.Id))]
    [MapperIgnoreTarget(nameof(SnapshotHistoryEntry.Sequence))]
    [MapperIgnoreTarget(nameof(SnapshotHistoryEntry.CreatedAt))]
    [MapperIgnoreTarget(nameof(SnapshotHistoryEntry.Actor))]
    [MapProperty(nameof(Snapshot.Id), nameof(SnapshotHistoryEntry.SnapshotId))]
    [MapPropertyFromSource(nameof(SnapshotHistoryEntry.Snapshot))]
    internal static partial SnapshotHistoryEntry ToEntity(Snapshot snapshot, JsonElement content, Guid? actorUserId);
}
