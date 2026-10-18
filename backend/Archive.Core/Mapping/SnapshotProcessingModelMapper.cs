using Archive.Core.Entities.Processing;
using Archive.Core.Models.Processing;
using Riok.Mapperly.Abstractions;

namespace Archive.Core.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
internal static partial class SnapshotProcessingModelMapper
{
    [MapProperty(nameof(SnapshotProcessing.SnapshotId), nameof(SnapshotProcessingState.Id))]
    internal static partial SnapshotProcessingState ToState(SnapshotProcessing processing,
        int? queuePosition, bool canCancel, Entities.SnapshotModerationStatus moderationStatus, bool isPublic);
}
