using System.ComponentModel.DataAnnotations;

namespace Archive.Worker.Capture;

public sealed class CrawlPolicyOptions
{
    public const string SectionName = "Archive:CrawlPolicy";

    public bool Enabled { get; set; } = true;

    [Required, StringLength(128, MinimumLength = 1), RegularExpression("^[A-Za-z0-9_-]+$")]
    public string UserAgentToken { get; set; } = "VoldechseArchiveBot";

    [Range(0d, 300d)]
    public double MinimumHostDelaySeconds { get; set; } = 2d;

    [Range(1, 60)]
    public int RequestTimeoutSeconds { get; set; } = 10;

    [Range(1024, 5_000_000)]
    public int MaxRobotsBytes { get; set; } = 1_000_000;

    public TimeSpan MinimumHostDelay => TimeSpan.FromSeconds(MinimumHostDelaySeconds);
    public TimeSpan RequestTimeout => TimeSpan.FromSeconds(RequestTimeoutSeconds);
}
