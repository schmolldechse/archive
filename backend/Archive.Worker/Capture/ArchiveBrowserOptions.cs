using System.ComponentModel.DataAnnotations;

namespace Archive.Worker.Capture;

public sealed class ArchiveBrowserOptions
{
    public const string SectionName = "Archive:Browser";

    public bool Headless { get; set; } = true;

    [Required, StringLength(512, MinimumLength = 1), RegularExpression("^[ -~]+$")]
    public string UserAgent { get; set; } = "ArchiveBot/1.0";

    [Range(1, 300)]
    public int ChallengeTimeoutSeconds { get; set; } = 45;

    [Range(1, 3)]
    public int ChallengeReloadAttempts { get; set; } = 1;

    public TimeSpan ChallengeTimeout => TimeSpan.FromSeconds(ChallengeTimeoutSeconds);
}
