using System.ComponentModel.DataAnnotations;
using Archive.Core.Jobs;

namespace Archive.Core.Options;

public sealed class ArchiveOptions
{
    public const string SectionName = "Archive";

    [Range(typeof(long), "1", "9223372036853775807")]
    public long MaxUploadBytes { get; set; } = 250_000_000L;

    [Range(1, 4_294_967)]
    public int MaxDurationSeconds { get; set; } = 120;

    [Range(typeof(long), "1", "9223372036854775807")]
    public long MaxStorageBytes { get; set; } = 250_000_000L;

    [Range(1, int.MaxValue)]
    public int MaxResources { get; set; } = 500;

    [Range(0, int.MaxValue)]
    public int MaxScrollRounds { get; set; } = 24;

    [Range(0, int.MaxValue)]
    public int QuietPeriodMilliseconds { get; set; } = 1_000;

    [Range(1, int.MaxValue)]
    public int MaxImportDepth { get; set; } = 32;

    [Range(1, int.MaxValue)]
    public int MaxImportObjects { get; set; } = 100_000;

    public CaptureLimits ToCaptureLimits() => new(
        TimeSpan.FromSeconds(MaxDurationSeconds), MaxStorageBytes, MaxResources, MaxScrollRounds,
        TimeSpan.FromMilliseconds(QuietPeriodMilliseconds));
}
