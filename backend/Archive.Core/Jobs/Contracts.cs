using Archive.Core.Entities;
using Microsoft.Extensions.Configuration;

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
    TimeSpan QuietPeriod)
{
    public static CaptureLimits FromConfiguration(IConfiguration configuration) => new(
        TimeSpan.FromSeconds(configuration.GetValue("Archive:MaxDurationSeconds", 120)),
        configuration.GetValue("Archive:MaxStorageBytes", 250_000_000L),
        configuration.GetValue("Archive:MaxResources", 500),
        configuration.GetValue("Archive:MaxScrollRounds", 24),
        TimeSpan.FromMilliseconds(configuration.GetValue("Archive:QuietPeriodMilliseconds", 1_000)));
}

public sealed record ProgressUpdate(ProgressStep Step, int Percent, string Message);
