using System.ComponentModel.DataAnnotations;

namespace Archive.Core.Options;

public sealed class ProcessingOptions
{
    public const string SectionName = "Processing";
    // Terminal handoff window; queued and running work never expires through this setting.
    [Range(5, 3600)] public int TerminalRetentionSeconds { get; set; } = 60;
    // Only the API-hosted cleanup loop consumes the following settings.
    [Range(1, 3600)] public int CleanupIntervalSeconds { get; set; } = 30;
    [Range(1, 1000)] public int CleanupBatchSize { get; set; } = 100;
}
