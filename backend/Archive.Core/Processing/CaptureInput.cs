using Archive.Core.Entities;

namespace Archive.Core.Processing;

// A short-lived engine input, not a second persisted metadata entity.
public sealed record CaptureInput(Guid Id, SourceType SourceType, string? OriginUrl, string? UploadedObjectKey);
