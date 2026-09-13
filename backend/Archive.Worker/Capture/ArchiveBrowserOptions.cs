namespace Archive.Worker.Capture;

public sealed record ArchiveBrowserOptions(
    bool Headless,
    string UserAgent,
    TimeSpan ChallengeTimeout,
    int ChallengeReloadAttempts)
{
    public static ArchiveBrowserOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("Archive:Browser");
        var userAgent = (section["UserAgent"] ?? "ArchiveBot/1.0").Trim();
        var timeoutSeconds = section.GetValue("ChallengeTimeoutSeconds", 45);
        var reloadAttempts = section.GetValue("ChallengeReloadAttempts", 1);

        if (userAgent.Length is < 1 or > 512 || userAgent.Any(c => c < ' ' || c > '~'))
            throw new InvalidOperationException("Archive:Browser:UserAgent muss 1 bis 512 druckbare ASCII-Zeichen enthalten.");
        if (timeoutSeconds is < 1 or > 300)
            throw new InvalidOperationException("Archive:Browser:ChallengeTimeoutSeconds muss zwischen 1 und 300 liegen.");
        if (reloadAttempts is < 1 or > 3)
            throw new InvalidOperationException("Archive:Browser:ChallengeReloadAttempts muss zwischen 1 und 3 liegen.");

        return new ArchiveBrowserOptions(section.GetValue("Headless", true), userAgent,
            TimeSpan.FromSeconds(timeoutSeconds), reloadAttempts);
    }
}
