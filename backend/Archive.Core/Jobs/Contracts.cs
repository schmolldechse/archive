namespace Archive.Core.Jobs;

public sealed record CaptureLimits(
    TimeSpan MaxDuration,
    long MaxStorageBytes,
    int MaxResources,
    int MaxScrollRounds,
    TimeSpan QuietPeriod);
