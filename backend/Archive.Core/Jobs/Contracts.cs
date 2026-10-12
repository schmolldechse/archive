using Archive.Core.Entities;

namespace Archive.Core.Jobs;

public sealed record ArchiveRequest(
    SourceType SourceType,
    string? OriginUrl,
    string Title,
    string? Description,
    IReadOnlyList<string> Tags,
    string? UploadedObjectKey);

public sealed record CaptureLimits(
    TimeSpan MaxDuration,
    long MaxStorageBytes,
    int MaxResources,
    int MaxScrollRounds,
    TimeSpan QuietPeriod);

public sealed record ProgressUpdate(ProgressStep Step, int Percent, string Message);
